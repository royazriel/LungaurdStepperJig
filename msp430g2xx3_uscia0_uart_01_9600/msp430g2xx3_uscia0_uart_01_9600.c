/* --COPYRIGHT--,BSD_EX
 * Copyright (c) 2012, Texas Instruments Incorporated
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * *  Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * *  Neither the name of Texas Instruments Incorporated nor the names of
 *    its contributors may be used to endorse or promote products derived
 *    from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 *******************************************************************************
 * 
 *                       MSP430 CODE EXAMPLE DISCLAIMER
 *
 * MSP430 code examples are self-contained low-level programs that typically
 * demonstrate a single peripheral function or device feature in a highly
 * concise manner. For this the code may rely on the device's power-on default
 * register values and settings such as the clock configuration and care must
 * be taken when combining code from several examples to avoid potential side
 * effects. Also see www.ti.com/grace for a GUI- and www.ti.com/msp430ware
 * for an API functional library-approach to peripheral configuration.
 *
 * --/COPYRIGHT--*/
//******************************************************************************
//   MSP430G2xx3 Demo - USCI_A0, 9600 UART Echo ISR, DCO SMCLK
//
//   Description: Echo a received character, RX ISR used. Normal mode is LPM0.
//   USCI_A0 RX interrupt triggers TX Echo.
//   Baud rate divider with 1MHz = 1MHz/9600 = ~104.2
//   ACLK = n/a, MCLK = SMCLK = CALxxx_1MHZ = 1MHz
//
//                MSP430G2xx3
//             -----------------
//         /|\|              XIN|-
//          | |                 |
//          --|RST          XOUT|-
//            |                 |
//            |     P1.2/UCA0TXD|------------>
//            |                 | 9600 - 8N1
//            |     P1.1/UCA0RXD|<------------
//
//   D. Dang
//   Texas Instruments Inc.
//   February 2011
//   Built with CCS Version 4.2.0 and IAR Embedded Workbench Version: 5.10
//******************************************************************************
#include <msp430.h>
#include <stdio.h>
#include <string.h>

#define HOME_OPCODE 0xF8
#define START_OPCODE 0xF9
#define STOP_OPCODE 0xFA

#define LED  BIT0
#define STEP BIT4
#define DIR	 BIT5
#define ENABLE BIT0
#define STEPS_PER_REVOLUTION 400
#define REVOLUTION_IN_DEGREE 360.0f
#define MAX_SPEED 	75.0f		//RPM



typedef struct __MessageFormat
{
   unsigned char opcode;
   float frequency;
   float dc;
   int positiveDeg;
   int negativeDeg;
   int speed;
   int homingDeg;
}__attribute__ ((packed))MessageFormat;

typedef enum _bool
{
	false = 0,
	true
}bool;

typedef enum
{
	etMachineStateInit = 1,
	etMachineStatePositiveAccel,
	etMachineStatePositiveSteady,
	etMachineStatePositiveDeccel,
	etMachineStateDutyOn,
	etMachineStateNegativeAccel,
	etMachineStateNegativeSteady,
	etMachineStateNegativeDeccel,
	etMachineStateDutyOff,

}MachineState;

typedef enum
{
	etDirectionCCWPositive = 1,
	etDirectionCWNegative

}Dirction;

static MessageFormat sMessage;
static bool sMessageReady = false;
static long sCountMs = 0;
static bool sStartJig = false;

void InitBoard()
{
	WDTCTL = WDTPW + WDTHOLD;                 // Stop WDT
	if (CALBC1_1MHZ==0xFF)					// If calibration constant erased
	{
		while(1);                               // do not load, trap CPU!!
	}
	DCOCTL = 0;                               // Select lowest DCOx and MODx settings
	BCSCTL1 = CALBC1_1MHZ;                    // Set DCO
	DCOCTL = CALDCO_1MHZ;
	P1SEL = BIT1 + BIT2 ;                     // P1.1 = RXD, P1.2=TXD
	P1SEL2 = BIT1 + BIT2 ;                    // P1.1 = RXD, P1.2=TXD
	UCA0CTL1 |= UCSSEL_2;                     // SMCLK
	UCA0BR0 = 104;                            // 1MHz 9600
	UCA0BR1 = 0;                              // 1MHz 9600
	UCA0MCTL = UCBRS0;                        // Modulation UCBRSx = 1
	UCA0CTL1 &= ~UCSWRST;                     // **Initialize USCI state machine**
	IE2 |= UCA0RXIE;                          // Enable USCI_A0 RX interrupt
	P1DIR |= BIT0 | BIT4 | BIT5;              // Set P1.0 Led P1.4 Step P1.5 Dir  - Direction Output
	P1OUT &= ~LED;
	P2DIR |= BIT0;
	CCTL0 = CCIE;                             // CCR0 interrupt enabled
	CCR0 = 1000;
	TACTL = TASSEL_2 + MC_1;                  // SMCLK, upmode
	__bis_SR_register( GIE);       // Enter LPM0, interrupts enabled
}

void DelayMs( int ms )
{
	volatile long start = sCountMs;
	while( (sCountMs-start) < ms );
}

void HandleMessage( char rxByte )
{
	static int count = 0;
	int size = sizeof(MessageFormat);

	if( ( rxByte >=HOME_OPCODE && rxByte <= STOP_OPCODE ) && count == 0 && !sMessageReady)
	{
		*((unsigned char*)&sMessage) = rxByte;
		count++;

		P2OUT &= ~ENABLE;  //active low
		return;
	}

	if(count>0 && count< size )
	{
		*((unsigned char*)&sMessage + count) = rxByte;
		count++;
		P1OUT ^= LED;
	}
	if( count==size)
	{
		count = 0;
		sMessageReady = true;
	}
}

void GoFromZeroPositionToSelected( MessageFormat * msg,  Dirction dir)
{
	int negativeSteps = (int) (((float)msg->homingDeg/REVOLUTION_IN_DEGREE)* STEPS_PER_REVOLUTION)*2;
	int i;

	P1OUT |= STEP;

	if( dir == etDirectionCCWPositive) P1OUT &= ~DIR; else P1OUT |= DIR;

	for(i=negativeSteps; i>0; i--)
	{
		P1OUT ^= STEP;
		DelayMs(MAX_SPEED/(float) msg->speed);
	}
}

int main(void)
{
	MachineState state = etMachineStateInit;
	MessageFormat settings;
	long lastCycleStartTime;
	long lastStepStartTime;
	int positiveSteps;
	int negativeSteps;
	int delayForSpeed;
	long delayForDuty;
	long cyclePeriod;
	bool firstCycle = false;
	InitBoard();

	while(1)
	{
		if(sMessageReady == true )
		{
			memcpy( &settings, & sMessage, sizeof(MessageFormat));
			memset(&sMessage,0,sizeof(MessageFormat));
			sMessageReady = false;
			switch( settings.opcode )
			{
				case HOME_OPCODE:
					GoFromZeroPositionToSelected( &settings, etDirectionCWNegative);
					break;
				case START_OPCODE:
					state = etMachineStateInit;
					sStartJig = true;
					firstCycle = true;
					break;
			}
		}

		if( sStartJig )
		{
			switch( state )
			{
				case etMachineStateInit:
					if( settings.opcode == STOP_OPCODE )
					{
						sStartJig = false;
					}
					lastCycleStartTime = sCountMs;
					lastStepStartTime = sCountMs;
					cyclePeriod = 1000.0/settings.frequency;
					P1OUT |= STEP;
					P1OUT &= ~DIR;
					if( firstCycle )
					{
						GoFromZeroPositionToSelected( &settings, etDirectionCCWPositive);  //go back to home
						negativeSteps = 0;												   //first cycle is just positive
						firstCycle = false;
					}
					else
					{
						negativeSteps = (int) (((float)settings.negativeDeg/REVOLUTION_IN_DEGREE)* STEPS_PER_REVOLUTION)*2;
					}
					positiveSteps = negativeSteps + (int) (((float)settings.positiveDeg/REVOLUTION_IN_DEGREE)* STEPS_PER_REVOLUTION)*2;
					delayForSpeed = MAX_SPEED/(float) settings.speed;
					state++;
					break;
				case etMachineStatePositiveAccel:
					state++;
					break;
				case etMachineStatePositiveSteady:
					if(sCountMs - lastStepStartTime > delayForSpeed)
					{
						P1OUT ^= STEP;
						lastStepStartTime = sCountMs;
						positiveSteps--;
					}
					if(!positiveSteps)
					{
						state++;
						delayForDuty = (settings.dc * cyclePeriod)/100.0;
					}
					break;
				case etMachineStatePositiveDeccel:
					state++;
					break;
				case etMachineStateDutyOn:
					if(sCountMs - lastCycleStartTime > delayForDuty)
					{
						lastStepStartTime = sCountMs;
						P1OUT |= STEP;
						P1OUT |= DIR;
						positiveSteps = (int) (((float)settings.positiveDeg/REVOLUTION_IN_DEGREE)* STEPS_PER_REVOLUTION)*2;
						negativeSteps = positiveSteps + (int) (((float)settings.negativeDeg/REVOLUTION_IN_DEGREE)* STEPS_PER_REVOLUTION)*2;
						state++;
					}
					break;
				case etMachineStateNegativeAccel:
					state++;
					break;
				case etMachineStateNegativeSteady:
					if(sCountMs - lastStepStartTime > delayForSpeed)
					{
						P1OUT ^= STEP;
						lastStepStartTime = sCountMs;
						negativeSteps--;
					}
					if(!negativeSteps)
					{
						state++;
						delayForDuty+= ( (100.0 - settings.dc) * cyclePeriod)/100.0;
					}
					break;
				case etMachineStateNegativeDeccel:
					state++;
					break;
				case etMachineStateDutyOff:
					if(sCountMs - lastCycleStartTime > delayForDuty)
					{
						state = etMachineStateInit;
					}
					break;


			}
		}


	}

}

//  Echo back RXed character, confirm TX buffer is ready first
#pragma vector=USCIAB0RX_VECTOR
__interrupt void USCI0RX_ISR(void)
{
  while (!(IFG2&UCA0TXIFG));                // USCIjj_A0 TX buffer ready?
  HandleMessage(UCA0RXBUF);
}

// Timer_A3 Interrupt Vector (TA0IV) handler
#pragma vector=TIMER0_A0_VECTOR
__interrupt void Timer_A(void)
{
	sCountMs++;
}
