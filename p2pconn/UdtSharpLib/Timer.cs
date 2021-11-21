using System;
using System.Diagnostics;
using System.Threading;

namespace UdtSharp
{
    public class Timer
    {
        ulong m_ullSchedTime;             // next schedulled time
        static ulong s_ullCPUFrequency = readCPUFrequency();// CPU frequency : clock cycles per microsecond

        EventWaitHandle m_TickCond = new EventWaitHandle(false, EventResetMode.AutoReset);
        object m_TickLock = new object();

        static EventWaitHandle m_EventCond = new EventWaitHandle(false, EventResetMode.AutoReset);
        static object m_EventLock = new object();

        static bool m_bUseMicroSecond = false; // sepcial handling if timer frequency is low (< 10 ticks per microsecond)

        public Timer()
        {

        }

        public static ulong rdtsc()
        {
            if (m_bUseMicroSecond)
            {
                return getTime();
            }
            return (ulong)Stopwatch.GetTimestamp();
        }

        public void Stop()
        {
            m_TickCond.Close();
        }

        static ulong readCPUFrequency()
        {
            long ticksPerSecond = Stopwatch.Frequency;
            long ticksPerMicroSecond = ticksPerSecond / 1000000L;

            if (ticksPerMicroSecond < 10)
            {
                m_bUseMicroSecond = true;
                return 1;
            }

            return (ulong)ticksPerMicroSecond;
        }

        public static ulong getCPUFrequency()
        {
            // ticks per microsecond
            return (ulong)s_ullCPUFrequency;
        }

        void sleep(ulong interval)
        {
            ulong t = rdtsc();

            // sleep next "interval" time
            sleepto(t + interval);
        }

        public void sleepto(ulong nexttime)
        {
            // Use class member such that the method can be interrupted by others
            m_ullSchedTime = nexttime;

            ulong t = rdtsc();

            while (t < m_ullSchedTime)
            {
                m_TickCond.WaitOne(1);

                t = rdtsc();
            }
        }

        public void interrupt()
        {
            // schedule the sleepto time to the current CCs, so that it will stop
            m_ullSchedTime = rdtsc();
            tick();
        }

        public void tick()
        {
            m_TickCond.Set();
        }

        public static ulong getTime()
        {
            // microsecond resolution
            return (ulong)DateTime.Now.Ticks / 10;
        }

        public static void triggerEvent()
        {
            m_EventCond.Set();
        }

        static void waitForEvent()
        {
            m_EventCond.WaitOne(1);
        }

        static void sleep()
        {
            Thread.Sleep(1);
        }
    }
}