using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class PersonalDetailsForm : IDocument
{
    public string? JobTitle { get; set; }
    public string? Salary { get; set; }
    public string? JobStartDate { get; set; }
    public string? FullNameTH { get; set; }
    public string? NickNameTH { get; set; }
    public string? FullNameENG { get; set; }
    public string? NickNameENG { get; set; }
    public string? IDCard { get; set; }
    public string? BirthDate { get; set; }
    public int Age { get; set; }
    public string? Weight { get; set; }
    public string? Height { get; set; }
    public string? CurrentAddress { get; set; }
    public string? CurrentSubDistrict { get; set; }
    public string? CurrentDistrict { get; set; }
    public string? CurrentProvince { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            // page.Margin(20);
            page.MarginVertical(20); // บน+ล่าง
            page.MarginHorizontal(20); // ซ้าย+ขวา

            page.DefaultTextStyle(x => x.FontSize(11));

            // page.Header()
            //     .Text("Personal Details")
            //     .FontSize(16)
            //     .Bold()
            //     .AlignCenter();

            page.Header()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item().AlignCenter().Width(80)
                        .Image(Path.Combine("Views", "imagesform", "one_logo.png"));

                    col.Item().AlignCenter()
                        .PaddingTop(4).PaddingBottom(0)
                        .Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)")
                        .FontSize(8)
                        .Bold();

                    col.Item().AlignCenter()
                        .PaddingTop(2).PaddingBottom(0)
                        .Text("The ONE Enterprise Public Company Limited")
                        .FontSize(8);
                });

            page.Content().Column(col =>
             {

                 col.Spacing(0);
                 col.Item().PaddingBottom(0).Row(row =>
                 {
                     row.RelativeItem(4).AlignRight().Padding(5).Text($"วันที่พร้อมเริ่มงาน:  {JobStartDate}").FontSize(8);
                 });

                 col.Item().Border(1).BorderColor(Colors.Black).Column(innerCol =>
                 {
                     innerCol.Item().Row(row =>
                     {
                         row.RelativeItem(8)
                             .Padding(2)
                             .PaddingLeft(5)
                             .AlignMiddle()
                             .Text($"ตำแหน่งที่ต้องการสมัคร: {JobTitle}")
                             .FontSize(8);

                         row.RelativeItem(4)
                             .BorderLeft(1)
                             .BorderColor(Colors.Black)
                             .Padding(2)
                             .PaddingLeft(5)
                             .MinHeight(15)
                             .AlignMiddle()
                             .Text($"อัตราเงินเดือนที่ต้องการ: {Salary} บาท")
                             .FontSize(8);
                     });
                 });


                 col.Item().Padding(5).Text("ข้อมูลส่วนตัว (Personal Details)").FontSize(8);

                 col.Item().Border(1).BorderColor(Colors.Black).Padding(5).Column(innerCol =>
                 {
                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ชื่อ-สกุล: {FullNameTH}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"ชื่อเล่น: {NickNameTH}").FontSize(8);
                     });


                     innerCol.Item().Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"Name-Surname: {FullNameENG}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"Nick Name: {NickNameENG}").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem(4).Padding(5).Text($"บัตรประจำตัวประชาชน: {IDCard}").FontSize(8);
                         row.RelativeItem(3).Padding(5).Text($"วัน/เดือน/ปี เกิด: {BirthDate}").FontSize(8);
                         row.RelativeItem(2).Padding(5).Text($"อายุ: {Age} ปี").FontSize(8);
                         row.RelativeItem(2).Padding(5).Text($"น้ำหนัก: {Weight} กก.").FontSize(8);
                         row.RelativeItem(2).Padding(5).Text($"ส่วนสูง: {Height} ซม.").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ที่อยู่(ปัจจุบัน): {CurrentAddress}").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"แขวง/ตำบล: {CurrentSubDistrict}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"เขต/อำเภอ: {CurrentDistrict}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"จังหวัด: {CurrentProvince}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"รหัสไปรษณีย์: {ZipCode}").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"E-mail: {Email}").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"สถานภาพสมรส: โสด").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"จำนวนบุตร: 0").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ชื่อคู่สมรส: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"อาชีพ: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"บริษัท: ").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"E-mail: {Email}").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ชื่อบิดา: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"อายุ:  ปี").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"อาชีพ: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"เบอร์โทร: ").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ชื่อมารดา: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"อายุ: ปี").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"อาชีพ: ").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"เบอร์โทร: ").FontSize(8);
                     });

                     innerCol.Item().PaddingBottom(0).Row(row =>
                     {
                         row.RelativeItem().Padding(5).Text($"ท่านมีพี่-น้องจำนวน:  คน").FontSize(8);
                         row.RelativeItem().Padding(5).Text($"ท่านเป็นคนที่:  (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(8);
                     });
                 });

                 // Educational 

                 col.Item().Padding(5).Text("ข้อมูลประวัติการศึกษา (Educational Details)").FontSize(8);

                 col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                 {
                     table.ColumnsDefinition(columns =>
                     {
                         columns.RelativeColumn(3);  // ระดับการศึกษา
                         columns.RelativeColumn(4);  // ชื่อสถานศึกษา
                         columns.RelativeColumn(3);  // จังหวัด
                         columns.RelativeColumn(2);  // From
                         columns.RelativeColumn(2);  // To
                         columns.RelativeColumn(4);  // วุฒิ / สาขา
                         columns.RelativeColumn(2);  // GPA
                     });

                     // ===== Header Row 1 =====
                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ชื่อสถานศึกษา\n(Name of place)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("จังหวัด/ประเทศ\n(Province/Country)").FontSize(8);

                     // หัวข้อใหญ่ "ปีการศึกษา"
                     table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ปีการศึกษา\n(Graduated year)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("GPA").FontSize(8);

                     // ===== Header Row 2 (ใต้ปีการศึกษา) =====
                     table.Cell().Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                         .Text("ตั้งแต่ปี\n(From)").FontSize(8);

                     table.Cell().Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                         .Text("ถึงปี\n(To)").FontSize(8);

                     // ===== Rows (Data) =====
                     void AddRow(string level)
                     {
                         table.Cell().Border(1).Padding(3).Text(level).FontSize(8);
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text(""); // From
                         table.Cell().Border(1).Padding(3).Text(""); // To
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                     }

                     AddRow("มัธยมศึกษา (Secondary)");
                     AddRow("ประกาศนียบัตรวิชาชีพ (Vocational)");
                     AddRow("ปริญญาตรี (Bachelor)");
                     AddRow("ปริญญาโท (Master)");
                     AddRow("อื่น ๆ (Other)");
                 });

                 // Work Experiences
                 col.Item().Padding(5).Text("ข้อมูลประวัติการทำงาน (Work Experiences)").FontSize(8);

                 col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                 {
                     table.ColumnsDefinition(columns =>
                     {
                         columns.RelativeColumn(2);  // From
                         columns.RelativeColumn(2);  // To
                         columns.RelativeColumn(3);  // ระดับการศึกษา
                         columns.RelativeColumn(4);  // ชื่อสถานศึกษา
                         columns.RelativeColumn(3);  // จังหวัด
                         columns.RelativeColumn(4);  // วุฒิ / สาขา
                         columns.RelativeColumn(2);  // GPA
                     });

                     // ===== Header Row 1 =====
                     // หัวข้อใหญ่ "ปีการศึกษา"
                     table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ระยะเวลา\nPeriod").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("บริษัท\nCompany's Name").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ตำแหน่ง\nPosition").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ลักษณะงานโดยสังเขป\nJob descriptions").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("เหตุผลที่ลาออก\nReasion for leaving").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("เงินเดือนสุดท้าย").FontSize(8);

                     // ===== Header Row 2 (ใต้ปีการศึกษา) =====
                     table.Cell().Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                         .Text("ตั้งแต่ปี(From)\nMM/YY").FontSize(8);

                     table.Cell().Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                         .Text("ถึงปี(To)\nMM/YY").FontSize(8);

                     // ===== Rows (Data) =====
                     void AddRow(string level)
                     {
                         table.Cell().Border(1).Padding(3).Text(""); // From
                         table.Cell().Border(1).Padding(3).Text(""); // To
                         table.Cell().Border(1).Padding(3).Text(level).FontSize(8);
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                     }

                     AddRow("");
                     AddRow("");
                     AddRow("");
                     AddRow("");
                     AddRow("");

                 });

             });

            // page.Footer()
            //     .AlignCenter()
            //     .Text(x => x.CurrentPageNumber());

            page.Footer().AlignRight().Column(col =>
            {
                col.Item().Padding(5).Text("ลงชื่อผู้สมัคร  ............................................").FontSize(8);
                col.Item().Padding(5).Text("Signature  (..........................................)").FontSize(8);
                col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8);
            });
        });

        // ---------------------------------------------------- Page 2 ----------------------------------------------------

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            // page.Margin(20);
            page.MarginVertical(20); // บน+ล่าง
            page.MarginHorizontal(20); // ซ้าย+ขวา

            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item().AlignCenter().Width(80)
                        .Image(Path.Combine("Views", "imagesform", "one_logo.png"));

                    col.Item().AlignCenter()
                        .PaddingTop(4).PaddingBottom(0)
                        .Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)")
                        .FontSize(8)
                        .Bold();

                    col.Item().AlignCenter()
                        .PaddingTop(2).PaddingBottom(0)
                        .Text("The ONE Enterprise Public Company Limited")
                        .FontSize(8);
                });

            page.Content().Column(col =>
            {
                col.Item().Padding(5).Text("ความสามารถพิเศษ").FontSize(8);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Black).Column(col1 =>
                    {
                        col1.Item().Background(Colors.Grey.Lighten3).Padding(5)
                            .Text("ภาษาต่างประเทศ (Language)").FontSize(8);

                        col1.Item().Padding(5).Text("TOEIC: \n 600").FontSize(8);
                        col1.Item().Padding(5).Text("TOEFL: \n 100").FontSize(8);
                        col1.Item().Padding(5).Text("IELTS: \n 8").FontSize(8);
                        col1.Item().Padding(5).Text("ภาษาอื่นๆ: ").FontSize(8);
                    });

                    row.RelativeItem().PaddingLeft(5).Column(col2 =>
                    {

                        col2.Item().Border(1).BorderColor(Colors.Black).Column(box =>
                        {
                            box.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text("ความรู้ทางคอมพิวเตอร์ (Computer Skills)").FontSize(8);

                            for (int i = 0; i < 4; i++)
                            {
                                box.Item().PaddingHorizontal(5).Height(20)
                                    .BorderBottom(0.5f).BorderColor(Colors.Black);
                            }
                        });

                        col2.Item().Border(1).BorderColor(Colors.Black).Column(box =>
                        {
                            box.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text("ความรู้หรือทักษะอื่น ๆ (Other Skills)").FontSize(8);

                            for (int i = 0; i < 3; i++)
                            {
                                box.Item().PaddingHorizontal(5).Height(20)
                                    .BorderBottom(0.5f).BorderColor(Colors.Black);
                            }
                        });
                    });
                });

                col.Item().Padding(5).Text("บุคคลที่ติดต่อในกรณีเร่งด่วน").FontSize(8);
                col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                 {
                     table.ColumnsDefinition(columns =>
                     {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(4);
                     });

                     // ===== Header Row 1 =====
                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ชื่อ-สกุล\n(Name-Surname)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ความสัมพันธ์\n(Relation)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("โทรศัพท์\n(Tel.)").FontSize(8);

                     table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                         .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                         .Text("ที่อยู่\n(Address)").FontSize(8);

                     // ===== Rows (Data) =====
                     void AddRow(string level)
                     {
                         table.Cell().Border(1).Padding(3).Text(level).FontSize(8);
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                         table.Cell().Border(1).Padding(3).Text("");
                     }
                     AddRow("");
                 });
            });

            page.Footer().AlignRight().Column(col =>
            {
                col.Item().Padding(5).Text("ลงชื่อผู้สมัคร  ............................................").FontSize(8);
                col.Item().Padding(5).Text("Signature  (..........................................)").FontSize(8);
                col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8);
            });
        });
    }
}
