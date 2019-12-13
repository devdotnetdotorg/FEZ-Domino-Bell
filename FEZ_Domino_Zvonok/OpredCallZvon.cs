using System;
using Microsoft.SPOT;

namespace FEZ_Domino_Zvonok
{
    //Класс сравнения времени - часы минуты
    //Если возвращает - ок, необходимо подавать звонок
    public class OpredCallZvon
    {
        //время
        public DateTime privateNewTime;
        public DateTime privateOldTime;
        public DateTime privateZvonTime;
        //
        public bool isCall
        {
            get
            {
                // код аксессора для чтения из поля
                isCalFunc();
                return privateisCall;
            }
        }
        //
        public bool privateisCall;
        //
        public OpredCallZvon(DateTime NewTime, DateTime OldTime, DateTime ZvonTime)
        {
            privateNewTime = NewTime;
            privateOldTime = OldTime;
            privateZvonTime = ZvonTime;
            privateisCall = false;
        }
        private void isCalFunc()
        {
            //разделение на Час Минута
            int NewTimeMin = privateNewTime.Minute;
            int NewTimeHour = privateNewTime.Hour;
            int NewTimeSec = privateNewTime.Second;
            int OldTimeMin = privateOldTime.Minute;
            int OldTimeHour = privateOldTime.Hour;
            int OldTimeSec = privateOldTime.Second;
            int ZvonTimeMin = privateZvonTime.Minute;
            int ZvonTimeHour = privateZvonTime.Hour;
            int ZvonTimeSec = privateZvonTime.Second;
            //
            if (((ZvonTimeMin == 0) && (ZvonTimeHour == 0)) || (NewTimeHour == 0) || (OldTimeHour == 0))
            {
                //если нули во времени, звонок не подается
                privateisCall = false;
            }
            else
            {
                //проверка условия вхождения
                if ((NewTimeHour >= ZvonTimeHour) && (ZvonTimeHour >= OldTimeHour) &&
                    (NewTimeMin >= ZvonTimeMin) && (ZvonTimeMin >= OldTimeMin) &&
                    (NewTimeSec >= ZvonTimeSec) && (ZvonTimeSec >= OldTimeSec))
                {
                    privateisCall = true;
                }
                else
                {
                    privateisCall = false;
                }
            }
        }
    }
}
