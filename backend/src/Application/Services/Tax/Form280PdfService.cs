using Application.DTOs.Tax;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Services.Tax;

/// <summary>
/// PDF generation service for Form 280 (ITNS 280) - Advance Tax Challan
/// </summary>
public class Form280PdfService
{
    static Form280PdfService()
    {
        // QuestPDF Community license for open-source projects
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generate Form 280 challan as PDF
    /// </summary>
    public byte[] GenerateChallan(Form280ChallanDto challan)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, challan));
                page.Content().Element(c => ComposeContent(c, challan));
                page.Footer().Element(c => ComposeFooter(c, challan));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Form280ChallanDto challan)
    {
        container.Column(column =>
        {
            // Form Title
            column.Item().BorderBottom(2).BorderColor(Colors.Black).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("CHALLAN").Bold().FontSize(16);
                    c.Item().Text("ITNS 280").Bold().FontSize(14);
                    c.Item().Text("(0020) INCOME TAX ON COMPANIES (CORPORATION TAX)").FontSize(9);
                });

                row.ConstantItem(150).AlignRight().Column(c =>
                {
                    c.Item().Text("Tax Applicable").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text("(Tick One)").FontSize(7).FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(5).Row(r =>
                    {
                        r.AutoItem().Border(1).Padding(3).Text(challan.MinorHead == "100" ? "X" : " ").FontSize(8);
                        r.AutoItem().PaddingLeft(3).Text("(100) ADVANCE TAX").FontSize(8);
                    });
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Border(1).Padding(3).Text(challan.MinorHead == "300" ? "X" : " ").FontSize(8);
                        r.AutoItem().PaddingLeft(3).Text("(300) SELF ASSESSMENT TAX").FontSize(8);
                    });
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Border(1).Padding(3).Text(challan.MinorHead == "400" ? "X" : " ").FontSize(8);
                        r.AutoItem().PaddingLeft(3).Text("(400) TAX ON REGULAR ASSESSMENT").FontSize(8);
                    });
                });
            });

            column.Item().Height(10);
        });
    }

    private void ComposeContent(IContainer container, Form280ChallanDto challan)
    {
        container.Column(column =>
        {
            // Taxpayer Details Section
            column.Item().PaddingVertical(10).Element(c => ComposeTaxpayerDetails(c, challan));

            // Assessment Details Section
            column.Item().PaddingVertical(10).Element(c => ComposeAssessmentDetails(c, challan));

            // Payment Details Section
            column.Item().PaddingVertical(10).Element(c => ComposePaymentDetails(c, challan));

            // Bank Details Section (if applicable)
            if (!string.IsNullOrEmpty(challan.BankName))
            {
                column.Item().PaddingVertical(10).Element(c => ComposeBankDetails(c, challan));
            }

            // Instructions Section
            column.Item().PaddingVertical(10).Element(ComposeInstructions);
        });
    }

    private void ComposeTaxpayerDetails(IContainer container, Form280ChallanDto challan)
    {
        container.Border(1).BorderColor(Colors.Grey.Darken1).Column(column =>
        {
            // Section Header
            column.Item().Background(Colors.Grey.Lighten3).Padding(5).Text("TAXPAYER DETAILS").Bold().FontSize(10);

            column.Item().Padding(10).Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(10);

                // PAN
                grid.Item().Column(c =>
                {
                    c.Item().Text("Permanent Account Number (PAN)").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.Pan).Bold();
                });

                // TAN (if applicable)
                grid.Item().Column(c =>
                {
                    c.Item().Text("Tax Deduction Account Number (TAN)").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(string.IsNullOrEmpty(challan.Tan) ? "N/A" : challan.Tan);
                });

                // Company Name (full width)
                grid.Item(2).Column(c =>
                {
                    c.Item().Text("Full Name").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.CompanyName).Bold();
                });

                // Address (full width)
                grid.Item(2).Column(c =>
                {
                    c.Item().Text("Address").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.Address);
                });

                // City
                grid.Item().Column(c =>
                {
                    c.Item().Text("City").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.City);
                });

                // State & Pincode
                grid.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("State").FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().Border(1).Padding(5).Text(challan.State);
                    });
                    row.ConstantItem(10);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Pin Code").FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().Border(1).Padding(5).Text(challan.Pincode);
                    });
                });

                // Email & Phone
                grid.Item().Column(c =>
                {
                    c.Item().Text("Email").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.Email);
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("Phone").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.Phone);
                });
            });
        });
    }

    private void ComposeAssessmentDetails(IContainer container, Form280ChallanDto challan)
    {
        container.Border(1).BorderColor(Colors.Grey.Darken1).Column(column =>
        {
            // Section Header
            column.Item().Background(Colors.Grey.Lighten3).Padding(5).Text("ASSESSMENT DETAILS").Bold().FontSize(10);

            column.Item().Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Assessment Year").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.AssessmentYear).Bold().FontSize(12);
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Financial Year").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.FinancialYear).Bold().FontSize(12);
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Major Head Code").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.MajorHead).Bold().FontSize(12);
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Minor Head Code").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.MinorHead).Bold().FontSize(12);
                });
            });

            // Quarter info if applicable
            if (challan.Quarter.HasValue)
            {
                column.Item().Padding(10).PaddingTop(0).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Quarter").FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().Border(1).Padding(5).Text($"{challan.QuarterLabel} (Q{challan.Quarter})").Bold();
                    });

                    row.ConstantItem(20);

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Due Date").FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().Border(1).Padding(5).Text(challan.DueDate.ToString("dd-MMM-yyyy")).Bold();
                    });

                    row.ConstantItem(20);

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Cumulative % Required").FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().Border(1).Padding(5).Text($"{challan.CumulativePercentRequired}%");
                    });

                    row.ConstantItem(20);

                    row.RelativeItem();
                });
            }
        });
    }

    private void ComposePaymentDetails(IContainer container, Form280ChallanDto challan)
    {
        container.Border(1).BorderColor(Colors.Grey.Darken1).Column(column =>
        {
            // Section Header
            column.Item().Background(Colors.Grey.Lighten3).Padding(5).Text("PAYMENT DETAILS").Bold().FontSize(10);

            column.Item().Padding(10).Column(c =>
            {
                // Tax breakdown
                c.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().Text("Total Tax Liability").FontSize(9);
                    table.Cell().AlignRight().Text(FormatCurrency(challan.TotalTaxLiability));

                    table.Cell().Text("Less: TDS Credit").FontSize(9);
                    table.Cell().AlignRight().Text($"({FormatCurrency(challan.TdsCredit)})");

                    table.Cell().Text("Less: TCS Credit").FontSize(9);
                    table.Cell().AlignRight().Text($"({FormatCurrency(challan.TcsCredit)})");

                    table.Cell().Text("Less: Advance Tax Already Paid").FontSize(9);
                    table.Cell().AlignRight().Text($"({FormatCurrency(challan.AdvanceTaxPaid)})");

                    table.Cell().BorderTop(1).PaddingTop(5).Text("Net Tax Payable").Bold().FontSize(10);
                    table.Cell().BorderTop(1).PaddingTop(5).AlignRight().Text(FormatCurrency(challan.NetPayable)).Bold();
                });

                c.Item().Height(20);

                // Amount being paid
                c.Item().Background(Colors.Blue.Lighten5).Border(2).BorderColor(Colors.Blue.Darken2).Padding(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("AMOUNT BEING DEPOSITED").Bold().FontSize(10);
                        col.Item().PaddingTop(5).Text(challan.AmountInWords).FontSize(9).Italic();
                    });
                    row.ConstantItem(150).AlignRight().AlignMiddle().Text(FormatCurrency(challan.Amount)).Bold().FontSize(18);
                });

                // Payment date
                if (challan.PaymentDate.HasValue)
                {
                    c.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Date of Payment").FontSize(8).FontColor(Colors.Grey.Darken2);
                            col.Item().Border(1).Padding(5).Text(challan.PaymentDate.Value.ToString("dd-MMM-yyyy")).Bold();
                        });
                        row.ConstantItem(20);
                        row.RelativeItem(2);
                    });
                }
            });
        });
    }

    private void ComposeBankDetails(IContainer container, Form280ChallanDto challan)
    {
        container.Border(1).BorderColor(Colors.Grey.Darken1).Column(column =>
        {
            // Section Header
            column.Item().Background(Colors.Grey.Lighten3).Padding(5).Text("BANK DETAILS (For Office Use)").Bold().FontSize(10);

            column.Item().Padding(10).Grid(grid =>
            {
                grid.Columns(3);
                grid.Spacing(10);

                grid.Item().Column(c =>
                {
                    c.Item().Text("Name of Bank").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.BankName ?? "");
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("Branch").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.BranchName ?? "");
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("BSR Code").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.BsrCode ?? "");
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("Challan No.").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.ChallanNumber ?? "");
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("CIN").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.Cin ?? "");
                });

                grid.Item().Column(c =>
                {
                    c.Item().Text("Date of Deposit").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Border(1).Padding(5).Text(challan.PaymentDate?.ToString("dd-MMM-yyyy") ?? "");
                });
            });
        });
    }

    private void ComposeInstructions(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(5).Text("INSTRUCTIONS").Bold().FontSize(9);

            column.Item().Padding(10).Column(c =>
            {
                c.Item().Text("1. Use a separate challan for each type of tax payment.").FontSize(8);
                c.Item().Text("2. Quoting of PAN is mandatory for all taxpayers.").FontSize(8);
                c.Item().Text("3. Write your PAN/TAN on the reverse of the cheque/DD.").FontSize(8);
                c.Item().Text("4. Counterfoil will be returned to the taxpayer by the bank.").FontSize(8);
                c.Item().Text("5. For e-payment, use the Income Tax Department's e-filing portal.").FontSize(8);
                c.Item().PaddingTop(5).Text("Website: www.incometax.gov.in").FontSize(8).FontColor(Colors.Blue.Darken2);
            });
        });
    }

    private void ComposeFooter(IContainer container, Form280ChallanDto challan)
    {
        container.Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text($"Generated on: {challan.GeneratedAt:dd-MMM-yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text("This is a system generated challan. Please verify all details before payment.").FontSize(7).FontColor(Colors.Grey.Darken1);
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem();
                row.AutoItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("TAXPAYER'S / BANK'S ACKNOWLEDGEMENT COPY").Bold().FontSize(8);
                });
                row.RelativeItem();
            });
        });
    }

    private static string FormatCurrency(decimal amount)
    {
        return string.Format(new System.Globalization.CultureInfo("en-IN"), "{0:N0}", amount);
    }
}
