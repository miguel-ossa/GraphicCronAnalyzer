using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using COMException = System.Runtime.InteropServices.COMException;

namespace GraphicCronAnalyzer
{
    class CronFile
    {
        /// <summary>
        /// StreamReader utilizado para leer el fichero de datos.
        /// </summary>
        private StreamReader reader;
        /// <summary>
        /// Nombre del fichero de datos.
        /// </summary>
        private string sCronFilename;

        
        private class CronRegister
        {
            public string sMinute;
            public List<int> nMinutes;

            public string sHour;
            public List<int> nHours;

            public string sDay_of_month;
            public List<int> nDays_of_month;

            public string sMonth;
            public List<int> nMonths;

            public string sDay_of_week;
            public List<int> nDays_of_week;

            public List<string> listCommand;

            public bool bChecked;
            public bool bWillBeExecuted;
            
            public void CreateList()
            {
                listCommand = new List<string>();
                nMinutes = new List<int>();
                nHours = new List<int>();
                nDays_of_month = new List<int>();
                nMonths = new List<int>();
                nDays_of_week = new List<int>();
            }

            public override string ToString()
            {
                bool flag = false;
                string sResult = "[Minutes:";

                foreach (int myInt in nMinutes)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myInt.ToString();
                }
                sResult += " Hours:";
                flag = false;
                foreach (int myInt in nHours)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myInt.ToString();
                }
                sResult += " Days of month:";
                flag = false;
                foreach (int myInt in nDays_of_month)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myInt.ToString();
                }
                sResult += " Months:";
                flag = false;
                foreach (int myInt in nMonths)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myInt.ToString();
                }
                sResult += " Days of week:";
                flag = false;
                foreach (int myInt in nDays_of_week)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myInt.ToString();
                }
                sResult += " Command:";
                flag = false;
                foreach (string myCommand in listCommand)
                {
                    if (flag) sResult += " "; else flag = true;
                    sResult += myCommand;
                }
                sResult += "]";

                return sResult;
            }
        }
        List<CronRegister> myLinesList = new List<CronRegister>();

        public void GenerateTable(string FechaInicio, string FechaFin, string HoraInicio, string HoraFin)
        {
            DateTime dFecha = DateTime.Parse(FechaInicio + " " + HoraInicio);
            DateTime dFechaInicio = dFecha;
            DateTime dFechaFin = DateTime.Parse(FechaFin + " " + HoraFin);
            string sFecha = null;
            int contador = 0;

            object missing = Type.Missing;

            InitCheckedByCommand(dFecha, dFechaFin);

            int nLastDay = int.Parse(dFechaFin.ToString("dd"));
            int nLastMonth = int.Parse(dFechaFin.ToString("MM"));
            int nLastYear = int.Parse(dFechaFin.ToString("yyyy"));

            // Crear Excel
            Excel.Application oXL = new Excel.Application();
            oXL.Visible = false;
            Excel.Workbook oWB = oXL.Workbooks.Add(missing);
            Excel.Worksheet oSheet = oWB.ActiveSheet as Excel.Worksheet;
            while (dFecha <= dFechaFin)
            {
                if (dFecha == dFechaInicio)
                {
                    CreateSheet(oSheet, dFechaInicio, ref dFecha, dFechaFin, ref contador);
                }
                else if (dFecha <= dFechaFin)
                {
                    Excel.Worksheet oSheet2 = oWB.Sheets.Add(missing, missing, 1, missing)
                                        as Excel.Worksheet;
                    CreateSheet(oSheet2, dFechaInicio, ref dFecha, dFechaFin, ref contador);
                }
                int nDay = int.Parse(dFecha.ToString("dd"));
                int nMonth = int.Parse(dFecha.ToString("MM"));
                int nYear = int.Parse(dFecha.ToString("yyyy"));
                if ((nDay == nLastDay) &&
                    (nMonth == nLastMonth) &&
                    (nYear == nLastYear))
                {
                    CrearSh(dFechaFin);
                }
                /*
                if (dFecha <= dFechaFin)
                {
                    sFecha = dFecha.ToString("dd/MM/yyyy") + " " + "00:00:00";
                    dFecha = DateTime.Parse(sFecha);
                }
                 */
            }

            SortWs(ref oWB);
            oSheet.Select(missing);
            oXL.Visible = true;

            string fileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                        + "\\CronTable.xlsx";
            oXL.DisplayAlerts = false;
            // Recomendado sólo lectura
            oWB.SaveAs(fileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault,
                    missing, missing, true, false,
                    Excel.XlSaveAsAccessMode.xlNoChange,
                    Excel.XlSaveConflictResolution.xlLocalSessionChanges, missing, missing);
            //oWB.Close(missing, missing, missing);
            oXL.UserControl = true;
            //oXL.Quit();

        }

        private void CrearSh(DateTime dFechaInicio)
        {
            DateTime dFecha = dFechaInicio;
            string sFechaFin = dFecha.ToString("dd/MM/yyyy") + " " + "23:59:59";
            DateTime dFechaFin = DateTime.Parse(sFechaFin);

            int nDay = int.Parse(dFechaInicio.ToString("dd"));
            int nMonth = int.Parse(dFechaInicio.ToString("MM"));
            int nHour = int.Parse(dFechaInicio.ToString("HH"));
            int nMinute = int.Parse(dFechaInicio.ToString("mm"));
            int nDayOfWeek = NumberOfDayOfWeek(dFechaInicio);


            bool swFound = false;
            while (dFecha <= dFechaFin)
            {
                foreach (CronRegister myCronRegister in myLinesList)
                {
                    if (!myCronRegister.bWillBeExecuted)
                    {
                        swFound = false;
                        foreach (int dayOfWeek in myCronRegister.nDays_of_week)
                        {
                            if (nDayOfWeek == dayOfWeek)
                            {
                                swFound = true;
                                break;
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int month in myCronRegister.nMonths)
                            {
                                if (nMonth == month)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int dayOfMonth in myCronRegister.nDays_of_month)
                            {
                                if (nDay == dayOfMonth)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int hour in myCronRegister.nHours)
                            {
                                if (nHour == hour)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int minute in myCronRegister.nMinutes)
                            {
                                if (nMinute == minute)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            string sCommand = null;
                            bool flag = false;
                            foreach (string myCommand in myCronRegister.listCommand)
                            {
                                if (flag) sCommand += " "; else flag = true;
                                sCommand += myCommand;
                            }

                            myCronRegister.bWillBeExecuted = true;

                        }
                    }
                }
                DateTime newDate = dFecha.AddMinutes(1);
                dFecha = newDate;
                nHour = int.Parse(dFecha.ToString("HH"));
                nMinute = int.Parse(dFecha.ToString("mm"));
                if ((nHour == 0) && (nMinute == 0))
                {
                    nDay = int.Parse(dFecha.ToString("dd"));
                    nMonth = int.Parse(dFecha.ToString("MM"));
                    nDayOfWeek = NumberOfDayOfWeek(dFecha);
                }
            }

            // Fichero para ejecutar comandos
            FileStream fileStream = new FileStream(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                        + "\\CronTable.sh",
                                 FileMode.Create, FileAccess.Write, FileShare.None);
            StreamWriter sw = new StreamWriter(fileStream);

            foreach (CronRegister myCronRegister in myLinesList)
            {
                if ( (myCronRegister.bChecked) && (!myCronRegister.bWillBeExecuted))
                {
                    string sCommand = null;
                    bool flag = false;
                    foreach (string myCommand in myCronRegister.listCommand)
                    {
                        if (flag) sCommand += " "; else flag = true;
                        sCommand += myCommand;
                    }
                    sw.WriteLine(sCommand);
                }
            }
            sw.Close();
        }

        public void CloseExcel()
        {
        }

        private void SortWs(ref Microsoft.Office.Interop.Excel.Workbook oWB)
        {
            List<Excel.Worksheet> for_sort = new List<Excel.Worksheet>();

            foreach (Excel.Worksheet ws in oWB.Worksheets)
            {
                for_sort.Add(ws);
            }

            for_sort.Sort(delegate(Excel.Worksheet wst1, Excel.Worksheet wst2)
            {
                return wst1.Name.CompareTo(wst2.Name);//sort by worksheet's name
            });

            Excel.Worksheet ws1, ws2;

            for (int i = 0; i < for_sort.Count; i++)
            {
                for (int j = 1; j <= oWB.Worksheets.Count; j++)
                {
                    ws1 = (Excel.Worksheet)oWB.Worksheets[j];
                    if (for_sort[i].Name == ws1.Name)
                    {
                        ws2 = (Excel.Worksheet)oWB.Worksheets[i + 1];
                        ws1.Move(ws2, Type.Missing);
                    }
                }
            }
        }
        private void CreateSheet(Excel.Worksheet oSheet, DateTime dFechaInicio, ref DateTime dFecha, DateTime dFechaFin, ref int contador)
        {
            bool swFound = false;
            int nDay = int.Parse(dFecha.ToString("dd"));
            int nMonth = int.Parse(dFecha.ToString("MM"));
            int nHour = int.Parse(dFecha.ToString("HH"));
            int nMinute = int.Parse(dFecha.ToString("mm"));
            int nHourIni = nHour;
            int nMinuteIni = nMinute;
            int nHourFin = int.Parse(dFechaFin.ToString("HH"));
            int nMinuteFin = int.Parse(dFechaFin.ToString("mm"));
            int nDayOfWeek = NumberOfDayOfWeek(dFecha);
            object missing = Type.Missing;
            //string sFechaInicio = null;
            //string sFechaFin = null;


            // Crear columna 1 con las programaciones de los comandos
            oSheet.Name = dFecha.ToString("dd MM yyyy");
            int nRow = 1;
            oSheet.Cells[nRow, 1] = "Programación";
            foreach (CronRegister myCronRegister in myLinesList)
            {
                if (myCronRegister.bChecked)
                {
                    nRow++;
                    oSheet.Cells[nRow, 1] = myCronRegister.sMinute + " " +
                                            myCronRegister.sHour + " " +
                                            myCronRegister.sDay_of_month + " " +
                                            myCronRegister.sMonth + " " +
                                            myCronRegister.sDay_of_week;
                }
            }

            // Crear columna 2 con los nombres de los comandos
            nRow = 1;
            oSheet.Cells[nRow, 2] = "Comando";
            foreach (CronRegister myCronRegister in myLinesList)
            {
                if (myCronRegister.bChecked)
                {
                    nRow++;
                    string sCommand = null;
                    bool flag = false;
                    foreach (string myCommand in myCronRegister.listCommand)
                    {
                        if (flag) sCommand += " "; else flag = true;
                        sCommand += myCommand;
                    }
                    oSheet.Cells[nRow, 2] = sCommand;
                }
            }

            int nCol = 2; int nMaxCol = nCol;
            while (dFecha <= dFechaFin)
            {
                nCol++;
                if (nMaxCol < nCol)
                    nMaxCol = nCol;
                //((Microsoft.Office.Interop.Excel.Range)oSheet.Cells[1, nCol]).Name = "H" + dFecha.ToString("HH") + "M" + dFecha.ToString("mm");
                //((Microsoft.Office.Interop.Excel.Range)oSheet.Cells[1, nCol]).
                // Establecer HH:MM en la columna
                oSheet.Cells[1, nCol] = "[" + dFecha.ToString("HH") + ":" + dFecha.ToString("mm") + "]";
                //oSheet.Cells.Name = "H" + dFecha.ToString("HH") + "M" + dFecha.ToString("mm");

                nRow = 1;
                foreach (CronRegister myCronRegister in myLinesList)
                {
                    if (myCronRegister.bChecked)
                    {
                        swFound = false;
                        foreach (int dayOfWeek in myCronRegister.nDays_of_week)
                        {
                            if (nDayOfWeek == dayOfWeek)
                            {
                                swFound = true;
                                break;
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int month in myCronRegister.nMonths)
                            {
                                if (nMonth == month)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int dayOfMonth in myCronRegister.nDays_of_month)
                            {
                                if (nDay == dayOfMonth)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int hour in myCronRegister.nHours)
                            {
                                if (nHour == hour)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        if (swFound)
                        {
                            swFound = false;
                            foreach (int minute in myCronRegister.nMinutes)
                            {
                                if (nMinute == minute)
                                {
                                    swFound = true;
                                    break;
                                }
                            }
                        }
                        nRow++;
                        if (swFound)
                        {
                            oSheet.Cells[nRow, nCol] = "X";
                            oSheet.Cells[nRow, nCol].Name = "H" + dFecha.ToString("HH") + "_" + dFecha.ToString("mm") + "_" + contador.ToString();
                            contador++;
                        }
                        
                        //oSheet.Cells[nRow, nCol].Name = "pepe_" + contador.ToString();
                        //try
                        //{
                        
                        //}
                        //catch (Exception ex)
                        //{
                        //    Console.WriteLine(ex.Message);
                        //}
                        //=CONTAR.SI(RN2:RN202;"X")
                    }
                }
                //contador = 1;
                DateTime newDate = dFecha.AddMinutes(1);
                dFecha = newDate;
                nHour = int.Parse(dFecha.ToString("HH"));
                nMinute = int.Parse(dFecha.ToString("mm"));
                if ((nMinute > nMinuteFin) && (nHour == nHourFin))
                {
                    newDate = dFecha.AddDays(1);
                    dFecha = newDate;
                    dFecha = DateTime.Parse(newDate.ToString("yyyy/MM/dd") + " " + dFechaInicio.ToString("HH:mm"));
                    break;
                }

                if ((nHour == 0) && (nMinute == 0)) 
                    break;
            }

            nCol = 3;
            while (nCol <= nMaxCol)
            {
                string colInicial = oSheet.Columns[nCol].Address + "2";
                colInicial = colInicial.Substring(4);
                string colFinal = oSheet.Columns[nCol].Address + nRow.ToString();
                colFinal = colFinal.Substring(4);
                string colFormula = oSheet.Columns[nCol].Address + (nRow+1).ToString();
                colFormula = colFormula.Substring(4);
                try
                {
                    //oSheet.Cells[nRow + 1, nCol].Formula = "=CONTAR.SI(" + colInicial + ":" + colFinal + ";\"X\")";
                    Excel.Range rangeFormula = oSheet.get_Range(colFormula, colFormula);
                    rangeFormula.Formula = "=COUNTIF(" + colInicial + ":" + colFinal + "," + '"' + "X" + '"' + ")";
                }
                catch (COMException ex)
                {
                    Console.Write(ex.ErrorCode);
                }
                nCol++;
            }
            Excel.Range last = oSheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, missing);
            Excel.Range rangeAll = oSheet.get_Range("A1", last);
            Excel.Range rangeHours = oSheet.get_Range("C1", last);
            Excel.Range rangeA2 = oSheet.get_Range("A2", "A2");
            Excel.Range rangeProg = oSheet.get_Range("A1", "A1");
            Excel.Range rangeCommand = oSheet.get_Range("B1", "B1");
            //rangeAll.Columns.AutoFit();
            rangeProg.Columns.ColumnWidth = 80;
            rangeCommand.Columns.ColumnWidth = 100;
            rangeHours.Columns.ColumnWidth = 9;
            //rangeProgram.Columns.AutoFit();
            //range.Columns.NumberFormat = "@";
            rangeAll.AutoFilter(1, missing, Excel.XlAutoFilterOperator.xlAnd, missing, true);
            rangeA2.Activate();
            rangeA2.Select();
            rangeA2.Application.ActiveWindow.FreezePanes = true;
        }

        private void InitCheckedByCommand(DateTime dFechaInicio, DateTime dFechaFin)
        {
            DateTime dFecha = dFechaInicio;

            int nDay = int.Parse(dFechaInicio.ToString("dd"));
            int nMonth = int.Parse(dFechaInicio.ToString("MM"));
            int nHour = int.Parse(dFechaInicio.ToString("HH"));
            int nMinute = int.Parse(dFechaInicio.ToString("mm"));
            int nDayOfWeek = NumberOfDayOfWeek(dFechaInicio);

            //Inicializar checked
            foreach (CronRegister myCronRegister in myLinesList)
            {
                myCronRegister.bChecked = false;
                myCronRegister.bWillBeExecuted = false;
            }

            bool swFound = false;
            while (dFecha <= dFechaFin)
            {

                foreach (CronRegister myCronRegister in myLinesList)
                {
                    swFound = false;
                    foreach (int dayOfWeek in myCronRegister.nDays_of_week)
                    {
                        if (nDayOfWeek == dayOfWeek)
                        {
                            swFound = true;
                            break;
                        }
                    }
                    if (swFound)
                    {
                        swFound = false;
                        foreach (int month in myCronRegister.nMonths)
                        {
                            if (nMonth == month)
                            {
                                swFound = true;
                                break;
                            }
                        }
                    }
                    if (swFound)
                    {
                        swFound = false;
                        foreach (int dayOfMonth in myCronRegister.nDays_of_month)
                        {
                            if (nDay == dayOfMonth)
                            {
                                swFound = true;
                                break;
                            }
                        }
                    }
                    if (swFound)
                    {
                        swFound = false;
                        foreach (int hour in myCronRegister.nHours)
                        {
                            if (nHour == hour)
                            {
                                swFound = true;
                                break;
                            }
                        }
                    }
                    if (swFound)
                    {
                        swFound = false;
                        foreach (int minute in myCronRegister.nMinutes)
                        {
                            if (nMinute == minute)
                            {
                                swFound = true;
                                break;
                            }
                        }
                    }
                    if (swFound)
                    {
                        myCronRegister.bChecked = true;
                    }
                }
                DateTime newDate = dFecha.AddMinutes(1);
                dFecha = newDate;
                nHour = int.Parse(dFecha.ToString("HH"));
                nMinute = int.Parse(dFecha.ToString("mm"));
                if ((nHour == 0) && (nMinute == 0))
                {
                    nDay = int.Parse(dFecha.ToString("dd"));
                    nMonth = int.Parse(dFecha.ToString("MM"));
                    nDayOfWeek = NumberOfDayOfWeek(dFecha);
                }
            }
        }

        public int NumberOfDayOfWeek(DateTime fecha)
        {
            int day = 0;
            switch (fecha.DayOfWeek)
            {
                case DayOfWeek.Monday: day = 1; break;
                case DayOfWeek.Tuesday: day = 2; break;
                case DayOfWeek.Wednesday: day = 3; break;
                case DayOfWeek.Thursday: day = 4; break;
                case DayOfWeek.Friday: day = 5; break;
                case DayOfWeek.Saturday: day = 6; break;
                case DayOfWeek.Sunday: day = 0; break;
            }
            return day;
        }

        public void DumpCronRegister()
        {
            foreach (CronRegister myCronRegister in myLinesList)
            {
                Console.WriteLine(myCronRegister.ToString());
            }
        }

        /// <summary>
        /// Obtiene o establece CronFilename.
        /// </summary>
        /// <value>CronFilename.</value>
        public string CronFilename
        {
            get { return sCronFilename; }
            set { sCronFilename = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CronFile() 
        {
            sCronFilename = null;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        public CronFile(string s) 
        {
            sCronFilename = s;
        }

        public Boolean Exists()
        {
            if (File.Exists(sCronFilename))
                return true;
            else            
                return false;
        }

        /// <summary>
        /// Comprobar que el formato sea el de un fichero de cron.
        /// </summary>
        /// <returns>
        /// <c>true</c> si es correcto; en caso contrario, <c>false</c>.
        /// </returns>
        public Boolean CheckFile()
        {
            string sLine = null;
            char[] charSeparatorsColumns = new char[] { ' ' };
            int nLine = 0;

            try
            {
                //reader = new StreamReader(p_sNombreFicheroTexto);
                FileStream fs = new FileStream(sCronFilename, FileMode.Open, FileAccess.Read);
                reader = new StreamReader(sCronFilename, Encoding.Default);
                while (ReadNextLine(ref sLine))
                {
                    nLine++;
                    sLine = sLine.Trim();
                    if ((sLine != "") && (!sLine.StartsWith("#")))
                    {
                        string[] arColumnas = sLine.Split(charSeparatorsColumns, StringSplitOptions.None);
                        if (arColumnas.Length < 6)
                        {
                            return false;
                        }

                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepción " + ex.Message + " en Generador.AbrirFicheroTexto()");
                Console.ReadKey();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Leer la siguiente línea del fichero de cron.
        /// </summary>
        /// <param name="p_sLine">String utilizada para guardar el contenido de la línea.</param>
        /// <returns>
        /// <c>true</c> si pudo leerla; en caso contrario, <c>false</c>.
        /// </returns>
        private bool ReadNextLine(ref string p_sLine)
        {
            if (!reader.EndOfStream)
            {
                string strLine = reader.ReadLine();

                p_sLine = strLine;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Almacenar el contenido del fichero de cron.
        /// </summary>
        public void StoreLines()
        {
            string sLine = null;
            char[] charSeparatorsColumns = new char[] { ' ' };
            FileStream fs = new FileStream(sCronFilename, FileMode.Open, FileAccess.Read);
            reader = new StreamReader(sCronFilename, Encoding.Default);
            //int contador = 0;
            while (ReadNextLine(ref sLine))
            {
                //contador++;
                //Console.WriteLine("Contador=" + contador);
                sLine = sLine.Trim();
                if ((sLine != "") && (!sLine.StartsWith("#")))
                {
                    string[] arColumnas = sLine.Split(charSeparatorsColumns, StringSplitOptions.None);

                    CronRegister myCronRegister = new CronRegister();
                    myCronRegister.CreateList();
                    myCronRegister.sMinute = arColumnas[0];
                    myCronRegister.nMinutes = ExpandNumbers(myCronRegister.sMinute, 0, 59);
                    myCronRegister.sHour = arColumnas[1];
                    myCronRegister.nHours = ExpandNumbers(myCronRegister.sHour, 0, 59);
                    myCronRegister.sDay_of_month = arColumnas[2];
                    myCronRegister.nDays_of_month = ExpandNumbers(myCronRegister.sDay_of_month, 1, 31);
                    myCronRegister.sMonth = arColumnas[3];
                    myCronRegister.nMonths = ExpandNumbers(myCronRegister.sMonth, 1, 12);
                    myCronRegister.sDay_of_week = arColumnas[4];
                    myCronRegister.nDays_of_week = ExpandNumbers(myCronRegister.sDay_of_week, 0, 6);
                    //Console.WriteLine("-------------------------");
                    for (int i = 5; i < arColumnas.Length; i++)
                    {
                        string sString = arColumnas[i].ToString();
                        myCronRegister.listCommand.Add(sString);
                        //Console.WriteLine(sString);
                    }
                    //Console.WriteLine("-------------------------");

                    myLinesList.Add(myCronRegister);
                }
            }
            reader.Close();
        }

        private List<int> ExpandNumbers(string sRange, int nMin, int nMax)
        {
            char[] charSeparatorsColumns1 = new char[] { ',' };
            char[] charSeparatorsColumns2 = new char[] { '-' };
            List<int> myNumbersList = new List<int>();

            sRange = sRange.Trim();
            if (sRange == "*")
            {
                for (int i = nMin; i <= nMax; i++)
                {
                    myNumbersList.Add(i);
                }
            }
            else
            {
                string[] myTempString1 = sRange.Split(charSeparatorsColumns1, StringSplitOptions.None);

                for (int i = 0; i < myTempString1.Length; i++)
                {
                    if (myTempString1[i].Contains("-"))
                    {
                        string[] myTempString2 = myTempString1[i].Split(charSeparatorsColumns2, StringSplitOptions.None);
                        // Expandimos n-k desde n hasta k en incrementos de unidad
                        for (int k = int.Parse(myTempString2[0]); k <= int.Parse(myTempString2[1]); k++)
                        {
                            myNumbersList.Add(k);
                        }
                    }
                    else
                    {
                        myNumbersList.Add(int.Parse(myTempString1[i])); 
                    }
                }
            }

            return myNumbersList;
        }

    }
}
