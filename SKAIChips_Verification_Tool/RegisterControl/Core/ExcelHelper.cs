using ClosedXML.Excel;

namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public static class ExcelHelper
    {

        public static string[,] WorksheetToArray(IXLWorksheet ws)
        {
            if (ws == null)
                return new string[0, 0];

            var range = ws.RangeUsed();
            if (range == null)
                return new string[0, 0];

            var rows = range.RowCount();
            var cols = range.ColumnCount();

            var data = new string[rows, cols];

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var s = range.Cell(r + 1, c + 1).GetString();
                    s = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
                    data[r, c] = s;
                }
            }

            return data;
        }

    }
}
