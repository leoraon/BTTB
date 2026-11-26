using ClosedXML.Excel;

namespace CMMS.Backend.Helpers.Exporters;

public class ExcelReportExporter : IReportExporter
{
    public string FormatName => "excel";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => ".xlsx";

    public byte[] Export(string title, string subtitle, List<string> headers, List<List<string>> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BaoCao");
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 10;

        int colCount = Math.Max(3, headers.Count);
        int leftEnd = colCount / 2;
        int rightStart = leftEnd + 1;

        // Header hành chính 2 cột chuẩn ISO CMMS Việt Nam
        ws.Cell(1, 1).Value = "NHÀ MÁY GIA CÔNG CƠ KHÍ CMMS";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        ws.Cell(2, 1).Value = "Phòng Kỹ thuật – Bảo trì thiết bị";
        ws.Cell(2, 1).Style.Font.FontSize = 9;

        ws.Cell(1, rightStart).Value = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM";
        ws.Cell(1, rightStart).Style.Font.Bold = true;
        ws.Cell(1, rightStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, rightStart).Value = "Độc lập - Tự do - Hạnh phúc";
        ws.Cell(2, rightStart).Style.Font.Bold = true;
        ws.Cell(2, rightStart).Style.Font.Underline = XLFontUnderlineValues.Single;
        ws.Cell(2, rightStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (leftEnd > 1)
        {
            ws.Range(1, 1, 1, leftEnd).Merge();
            ws.Range(2, 1, 2, leftEnd).Merge();
        }
        if (colCount > rightStart)
        {
            ws.Range(1, rightStart, 1, colCount).Merge();
            ws.Range(2, rightStart, 2, colCount).Merge();
        }

        // Tiêu đề báo cáo & Kỳ báo cáo
        ws.Cell(4, 1).Value = title;
        ws.Cell(4, 1).Style.Font.Bold = true;
        ws.Cell(4, 1).Style.Font.FontSize = 13;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(4, 1, 4, colCount).Merge();

        ws.Cell(5, 1).Value = subtitle;
        ws.Cell(5, 1).Style.Font.Italic = true;
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(5, 1, 5, colCount).Merge();

        const int headerRow = 7;
        for (int c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#334155");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(headerRow).Height = 24;

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            bool striped = r % 2 == 1;
            for (int c = 0; c < headers.Count; c++)
            {
                var cell = ws.Cell(headerRow + 1 + r, c + 1);
                cell.Value = c < row.Count ? row[c] : string.Empty;
                if (striped) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        // Khối chữ ký 3 cấp chuẩn Việt Nam ở cuối báo cáo
        int sigStart = headerRow + rows.Count + 3;
        ws.Cell(sigStart - 1, rightStart).Value = $"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}";
        ws.Cell(sigStart - 1, rightStart).Style.Font.Italic = true;
        ws.Cell(sigStart - 1, rightStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        if (colCount > rightStart) ws.Range(sigStart - 1, rightStart, sigStart - 1, colCount).Merge();

        int col1 = 1;
        int col2 = Math.Max(2, (colCount + 1) / 2);
        int col3 = colCount;

        void WriteSig(int col, string role, string hint)
        {
            ws.Cell(sigStart, col).Value = role;
            ws.Cell(sigStart, col).Style.Font.Bold = true;
            ws.Cell(sigStart, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(sigStart + 1, col).Value = hint;
            ws.Cell(sigStart + 1, col).Style.Font.Italic = true;
            ws.Cell(sigStart + 1, col).Style.Font.FontSize = 9;
            ws.Cell(sigStart + 1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        WriteSig(col1, "NGƯỜI LẬP BIỂU", "(Ký, ghi rõ họ tên)");
        WriteSig(col2, "TRƯỞNG BỘ PHẬN BẢO TRÌ", "(Kiểm tra, xác nhận)");
        WriteSig(col3, "QUẢN LÝ NHÀ MÁY", "(Phê duyệt)");

        ws.Columns().AdjustToContents(12, 48);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
