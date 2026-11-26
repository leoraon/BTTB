using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CMMS.Backend.Helpers.Exporters;

public class PdfReportExporter : IReportExporter
{
    static PdfReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string FormatName => "pdf";
    public string ContentType => "application/pdf";
    public string FileExtension => ".pdf";

    public byte[] Export(string title, string subtitle, List<string> headers, List<List<string>> rows)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("NHÀ MÁY GIA CÔNG CƠ KHÍ CMMS").Bold().FontSize(10).FontColor("#1E3A8A");
                            left.Item().Text("Phòng Kỹ thuật – Bảo trì thiết bị").FontSize(8.5f).FontColor("#475569");
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(9.5f);
                            right.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(8.5f).Underline();
                        });
                    });

                    col.Item().PaddingTop(8).AlignCenter().Text(title).Bold().FontSize(13.5f).FontColor("#0F172A");
                    col.Item().AlignCenter().Text(subtitle).Italic().FontSize(8.5f).FontColor("#475569");
                });

                page.Content().PaddingTop(10).Column(body =>
                {
                    body.Item().Table(table =>
                    {
                        int colCount = Math.Max(1, headers.Count);
                        table.ColumnsDefinition(cols =>
                        {
                            for (int i = 0; i < colCount; i++)
                                cols.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var col in headers)
                                h.Cell().Background("#0F172A").Border(0.5f).BorderColor("#334155").Padding(5)
                                    .Text(col).Bold().FontColor(Colors.White).FontSize(8.5f);
                        });

                        for (int r = 0; r < rows.Count; r++)
                        {
                            var row = rows[r];
                            var bg = r % 2 == 1 ? "#F8FAFC" : "#FFFFFF";
                            for (int i = 0; i < colCount; i++)
                            {
                                var val = i < row.Count ? row[i] : string.Empty;
                                table.Cell().Background(bg).Border(0.5f).BorderColor("#CBD5E1").Padding(4).Text(val);
                            }
                        }
                    });

                    // Khối chữ ký 3 cấp chuẩn Việt Nam ở cuối trang báo cáo
                    body.Item().PaddingTop(14).EnsureSpace(90).Column(sig =>
                    {
                        sig.Item().AlignRight().PaddingBottom(6)
                            .Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                            .Italic().FontSize(8.5f).FontColor("#475569");

                        sig.Item().Row(r =>
                        {
                            void SigBox(string role, string hint)
                            {
                                r.RelativeItem().AlignCenter().Column(c =>
                                {
                                    c.Item().Text(role).Bold().FontSize(9);
                                    c.Item().Text(hint).Italic().FontSize(8).FontColor("#64748B");
                                    c.Item().PaddingTop(28).Text("..............................").FontSize(8.5f);
                                });
                            }

                            SigBox("NGƯỜI LẬP BIỂU", "(Ký, ghi rõ họ tên)");
                            SigBox("TRƯỞNG BỘ PHẬN BẢO TRÌ", "(Kiểm tra, xác nhận)");
                            SigBox("QUẢN LÝ NHÀ MÁY", "(Phê duyệt)");
                        });
                    });
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Hệ thống CMMS Cơ khí • Trang ");
                    txt.CurrentPageNumber();
                    txt.Span(" / ");
                    txt.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
