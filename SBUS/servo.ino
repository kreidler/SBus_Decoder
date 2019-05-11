volatile uint8_t curr_chA=0;
volatile uint8_t curr_chB=0;
uint8_t cntA = 0;
uint8_t cntB = 0;
volatile uint8_t State_chA=0;
volatile uint8_t State_chB=0;

void Timer1_init()
{
  // 16bit
  TCCR1B   =   0;   //stop timer - Timer/Counter Control Register. The pre-scaler can be configured here.
  TCCR1A   =   0;
  TCNT1   =    0;   //setup - Timer/Counter Register. The actual timer value is stored here.
  TCCR1A   =   0;
  TCCR1B = 0<<CS12 | 1<<CS11 | 0<<CS10;
  TIMSK1 = (1<<OCIE1A) | (1<<OCIE1B);   // allow interrupt by ?coincidence?
  OCR1A = 0;        // Output Compare Register
  OCR1B = 0;
  // 8bit
  TCCR2B   =   0;   //stop timer
  TCCR2A   =   0;
  TCNT2   =    0;   //setup
  TCCR2A   =   0;
  TCCR2A = 1<<WGM21;
  TCCR2B = (1<<CS21)|(1<<CS22);        // CLK/256 
  TIMSK2 = 1<<OCIE2A;
  OCR2A = 249;

  TCNT1 = 0;
  UpdateBankA();
  UpdateBankB();
}

ISR(TIMER2_COMPA_vect)
{
  cntA++;
  if (cntA>OCR_BANK_A){
    // end of frame to bank A
    curr_chA = 0;
    if (PPM_A == 1)
    {
      // PPM generator
      CH1_off;
      State_chA=0;
      OCR1A = TCNT1 + 400;
    }
    else
    {
      if (ChannelsMapA[curr_chA] < 16)
        OCR1A = TCNT1 + (sBus.channels[ChannelsMapA[curr_chA]]+2000);
      else OCR1A = TCNT1 + sBus.Failsafe() * 2000 + 2000;
      // Addition for switching
      if (SW_AB == true && ChannelsMapSwitchA[curr_chA] == true && sBus.channels[ChannelsMapA[curr_chA]] >= 1023) //addition to avoid 0.4V on the pin
        ServoOff(curr_chA);
      else
        ServoOn(curr_chA);
    }
    cntA = 0;
  }
  
  cntB++;
  if (cntB>OCR_BANK_B){
    curr_chB = 0;
    if (PPM_B == 1)
    {
      CH9_off;
      State_chB=0;
      OCR1B = TCNT1 + 400;
    }
    else
    {
      if (ChannelsMapB[curr_chB] < 16)
        OCR1B = TCNT1 + (sBus.channels[ChannelsMapB[curr_chB]]+2000);
      else OCR1B = TCNT1 + sBus.Failsafe() * 2000 + 2000;
      // Addition for switching
      if (SW_AB == true && ChannelsMapSwitchB[curr_chB] == true && sBus.channels[ChannelsMapB[curr_chB]] >= 1023) //addition to avoid 0.4V on the pin
        ServoOff(curr_chB+8);
      else
        ServoOn(curr_chB+8);
    }
    cntB = 0;
  }
}

ISR (TIMER1_COMPA_vect)
{
  if (PPM_A == 1)
    UpdatePPMA();
  else 
    UpdateBankA();
}

ISR (TIMER1_COMPB_vect)
{
  if (PPM_B == 1)
    UpdatePPMB();
  else 
    UpdateBankB();
}

void UpdateBankA()
{  // Addition for switching
  if (SW_AB == true && ChannelsMapSwitchA[curr_chA] == true && sBus.channels[ChannelsMapA[curr_chA]] < 1023)
    ServoOn(curr_chA);
  else
    ServoOff(curr_chA);

  curr_chA++;
  if (curr_chA<ChannelsBankA) {
    if (ChannelsMapA[curr_chA] < 16)
      OCR1A = TCNT1 + (sBus.channels[ChannelsMapA[curr_chA]]+2000);
    else
      OCR1A = TCNT1 + sBus.Failsafe() * 2000 + 2000;
    if (SW_AB == true && ChannelsMapSwitchA[curr_chA] == true && sBus.channels[ChannelsMapA[curr_chA]] >= 1023) //addition to avoid 0.4V on the pin
      ServoOff(curr_chA);
    else
      ServoOn(curr_chA);
  }
}

void UpdateBankB()
{  // Addition for switching
  if (SW_AB == true && ChannelsMapSwitchB[curr_chB] == true && sBus.channels[ChannelsMapB[curr_chB]] < 1023)
    ServoOn(curr_chB+8);
  else
    ServoOff(curr_chB+8);

  curr_chB++;
  if (curr_chB<ChannelsBankB) {
    if (ChannelsMapB[curr_chB] < 16) 
      OCR1B = TCNT1 + (sBus.channels[ChannelsMapB[curr_chB]]+2000);
    else 
      OCR1B = TCNT1 + sBus.Failsafe() * 2000 + 2000;
    if (SW_AB == true && ChannelsMapSwitchB[curr_chB] == true && sBus.channels[ChannelsMapB[curr_chB]] >= 1023) //addition to avoid 0.4V on the pin
      ServoOff(curr_chB+8);
    else
      ServoOn(curr_chB+8);
  }
}

void ServoOn(uint8_t ch)
{
  switch(ch)
  {
    case 0: CH0_on; break;
    case 1: CH1_on; break;
    case 2: CH2_on; break;
    case 3: CH3_on; break;
    case 4: CH4_on; break;
    case 5: CH5_on; break;
    case 6: CH6_on; break;
    case 7: CH7_on; break;
    case 8: CH8_on; break;
    case 9: CH9_on; break;
    case 10: CH10_on; break;
    case 11: CH11_on; break;
    case 12: CH12_on; break;
    case 13: CH13_on; break;
    case 14: CH14_on; break;
    case 15: CH15_on; break;
  }
}

void ServoOff(uint8_t ch)
{
  switch(ch)
  {
    case 0: CH0_off; break;
    case 1: CH1_off; break;
    case 2: CH2_off; break;
    case 3: CH3_off; break;
    case 4: CH4_off; break;
    case 5: CH5_off; break;
    case 6: CH6_off; break;
    case 7: CH7_off; break;
    case 8: CH8_off; break;
    case 9: CH9_off; break;
    case 10: CH10_off; break;
    case 11: CH11_off; break;
    case 12: CH12_off; break;
    case 13: CH13_off; break;
    case 14: CH14_off; break;
    case 15: CH15_off; break;
  }
}

void UpdatePPMA()
{
  if (State_chA==0)
  {
    CH1_on;
    State_chA=1;
    if (curr_chA<ChannelsBankA){
      if (ChannelsMapA[curr_chA] < 16)
        OCR1A = TCNT1 + (sBus.channels[ChannelsMapA[curr_chA]]+2000-400);
      else
        OCR1A = TCNT1 + sBus.Failsafe() * 2000 + 2000-400;
    }
  }
  else
  {
    CH1_off;
    State_chA=0;
    OCR1A = TCNT1 + 400;
    curr_chA++;
  }
}

void UpdatePPMB()
{
  if (State_chB==0)
  {
    CH9_on;
    State_chB=1;
    if (curr_chB<ChannelsBankB){
      if (ChannelsMapB[curr_chB] < 16)
        OCR1B = TCNT1 + (sBus.channels[ChannelsMapB[curr_chB]]+2000-400);
      else
        OCR1B = TCNT1 + sBus.Failsafe() * 2000 + 2000-400;
    }
  }
  else
  {
    CH9_off;
    State_chB=0;
    OCR1B = TCNT1 + 400;
    curr_chB++;
  }
}

