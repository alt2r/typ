using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    public sealed class ScheduledAudioEvent
    {
        public long SampleTime;
        public Action Trigger;
    }
}
