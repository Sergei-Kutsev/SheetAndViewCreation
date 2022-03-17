using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITrainingViewsSchedules.Wrappers
{
    public class ScheduleWrapper
    {
        public ScheduleWrapper(ViewSchedule viewSchedule)
        {
            ViewSchedule = viewSchedule;
            Name = viewSchedule.Name;
        }
        public bool IsSelected { get; set; } //можно не объявлять в конструкторе тк bolean, сюда записывается были ли выбрана спецификация
        public ViewSchedule ViewSchedule { get; }
        public string Name { get; }
    }
}
