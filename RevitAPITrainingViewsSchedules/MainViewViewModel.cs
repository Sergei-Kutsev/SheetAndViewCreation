using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using RevitAPITraining_Library;
using RevitAPITrainingViewsSchedules.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITrainingViewsSchedules
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        private Document _doc;

        public IList<FamilySymbol> SheetsTitles { get; } //собираем все типоразмеры листов в моделе
        public FamilySymbol SelectedSheetTitle { get; set; } //в эту переменную сохраняем выбранный тип листа
        public int NumberOfSheets { get; set; } // количество создаваемых листов выбранного типа

        public List<View> AllViewsInModel { get; } //собираем все виды в моделе
        public View SelectedView { get; set; } //в эту переменную сохраняем выбранный вид

        public string DesignedByAuthor { get; set; } //в эту переменную сохраняем автора листа, который будет записан в штамп

        public DelegateCommand CreateSheets { get; } //создаем листы, размещем виды, записываем автора


        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _doc = _commandData.Application.ActiveUIDocument.Document;
            AllViewsInModel = ViewsUtils.GetAllViews(_doc);
            SheetsTitles = SheetUtils.GetSheets(commandData);
            CreateSheets = new DelegateCommand(OnCreateSheetsCommand);
        }

        private void OnCreateSheetsCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;//помогает выбирать элементы
            Document doc = uidoc.Document; //переменная помогает распаковать объект из reference в элемент

            if (NumberOfSheets == 0 || NumberOfSheets < 0 || SelectedSheetTitle == null)
                return;

            using (var ts = new Transaction(doc, "Create instance")) //транзакция создания
            {
                ts.Start();

                ViewSheet newSheet = null;

                for (int i = 0; i < NumberOfSheets; i++)
                {
                    if (SelectedView != null && NumberOfSheets > 1) //если листов больше 1
                    {
                        newSheet = ViewSheet.Create(doc, SelectedSheetTitle.Id); //создали лист

                        Parameter commentParameter = newSheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY); // выбираем параметр листа
                        commentParameter.Set(DesignedByAuthor); //записываем в параметр

                        UV location = new UV((newSheet.Outline.Max.U - newSheet.Outline.Min.U) / 2, //находим центр листа
                                          (newSheet.Outline.Max.V - newSheet.Outline.Min.V) / 2);

                        var view = SelectedView.Duplicate(ViewDuplicateOption.Duplicate); //скопировали выбранный пользователем вид

                        Viewport.Create(doc, newSheet.Id, view, new XYZ(location.U, location.V, 0)); //добавляем скопированный вид в центр листа
                    }
                    else if (SelectedView != null && NumberOfSheets == 1) //если лист 1
                    {
                        newSheet = ViewSheet.Create(doc, SelectedSheetTitle.Id); //создали лист
                        Parameter commentParameter = newSheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY); // выбираем параметр листа
                        commentParameter.Set(DesignedByAuthor); //записываем в параметр

                        UV location = new UV((newSheet.Outline.Max.U - newSheet.Outline.Min.U) / 2, //находим центр листа
                                              (newSheet.Outline.Max.V - newSheet.Outline.Min.V) / 2);

                        Viewport.Create(doc, newSheet.Id, SelectedView.Id, new XYZ(location.U, location.V, 0)); //добавляем выбранный пользователем вид в центр листа
                    }
                    else if (SelectedView == null && NumberOfSheets > 1)
                    {
                        newSheet = ViewSheet.Create(doc, SelectedSheetTitle.Id); //создали лист
                        Parameter commentParameter = newSheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY); // выбираем параметр листа
                        commentParameter.Set(DesignedByAuthor); //записываем в параметр
                    }
                }

                ts.Commit();
                RaiseCloseRequest();
            }
        }
        /*
        private void OnAddFilterCommand()
        {
            List<ScheduleWrapper> selectedSchedules = SheetsTitles.Where(s => s.IsSelected).ToList();

            if (selectedSchedules == null)
                return;

            using (var ts = new Transaction(_doc, "Add filter to schedule"))
            {
                ts.Start();
                foreach (var schedule in selectedSchedules) //проходимся по каждой выбранной спеке,
                {
                    var scheduleDef = schedule.ViewSchedule.Definition; //заходим в определение спеки и сохраняем это в переменную
                                                                        //если в спеке есть параметры, то это значение будет не пустым
                    if (scheduleDef == null) // если пустые, то пропускаем
                        continue;

                    ScheduleField field = FindFiled(schedule.ViewSchedule, ParameterName); //находим существующее поле спеки c именем ParameterName 
                    if (field == null) //если она равна ничему
                    {
                        SchedulableField schedulableField = FindSchedulableField(schedule.ViewSchedule, ParameterName);//то добавляем это поле 
                        if (schedulableField == null) //если такого поля нет в 
                            continue;//то переходим к следующей итерации
                        field = scheduleDef.AddField(schedulableField);
                    }

                    if (field == null)
                        continue;

                    var filter = new ScheduleFilter(field.FieldId, ScheduleFilterType.Equal, ParameterValue);
                    if (filter == null)
                        continue;

                    scheduleDef.AddFilter(filter);
                }
                ts.Commit(); 
            }
            RaiseCloseRequest();
        }

        private SchedulableField FindSchedulableField(ViewSchedule viewSchedule, string parameterName)
        {
            var schedulableField = viewSchedule.Definition.GetSchedulableFields()
                .FirstOrDefault(p => p.GetName(viewSchedule.Document) == parameterName);
            return schedulableField;
        }

        private ScheduleField FindFiled(ViewSchedule viewSchedule, string parameterName)
        {
            ScheduleDefinition definition = viewSchedule.Definition;
            var fieldCount = definition.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = definition.GetField(i); //выбрали поле
                var fieldName = field.GetName(); //выбрали имя этого поля

                if (fieldName == parameterName)//и если имя совпало с нашим именем, то мы и нашли нужный параметр
                    return definition.GetField(i);
            }
            return null;//если мы прошли по списку и ничего не нашли, то просто возвращаем null
        }
        */
        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
