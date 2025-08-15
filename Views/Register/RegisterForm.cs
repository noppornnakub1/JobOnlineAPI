using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class PersonalDetailsForm : IDocument
{
    public string JobTitle { get; set; }
    public string Salary { get; set; }
    public string JobStartDate { get; set; }
    public string FullNameTH { get; set; }
    public string NickNameTH { get; set; }
    public string FullNameENG { get; set; }
    public string NickNameENG { get; set; }
    public string IDCard { get; set; }
    public string BirthDate { get; set; }
    public int Age { get; set; }
    public string Weight { get; set; }
    public string height { get; set; }
    public string CurrentAddress { get; set; }
    public string CurrentSubDistrict { get; set; }
    public string CurrentDistrict { get; set; }
    public string CurrentProvince { get; set; }
    public string ZipCode { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }

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
                    col.Item().Width(80).Height(80).Image(Path.Combine("Views", "imagesform", "one_logo.png"));
                });

            page.Content().Column(col =>
            {
                
                col.Spacing(0);
                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem(4).Border(0).BorderColor(Colors.Black).AlignRight().Padding(5).Text($"วันที่พร้อมเริ่มงาน:  {JobStartDate}");
                    // row.RelativeItem(4).Border(0).BorderColor(Colors.Black).Padding(5).Row(innerRow =>
                    // {
                    //     innerRow.RelativeItem().AlignLeft().Text("วันที่พร้อมเริ่มงาน:");
                    //     innerRow.ConstantItem(100).Text(JobStartDate);
                    // });
                });
                
                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem(5).Border(1).BorderRight(0).BorderColor(Colors.Black).Padding(5).Text($"ตำแหน่งที่ต้องการสมัคร: {JobTitle}");
                    row.RelativeItem(3).Border(1).BorderColor(Colors.Black).Padding(5).Text($"อัตราเงินเดือนที่ต้องการ: {Salary} บาท");
                });
                col.Spacing(4);
                col.Item().Text("ข้อมูลส่วนตัว (Personal Details)").FontSize(14);
                col.Spacing(4);
                col.Spacing(0);
                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อ-สกุล: {FullNameTH}");
                    row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อเล่น: {NickNameTH}");
                });

                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"Name-Surname: {FullNameENG}");
                    row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"Nick Name: {NickNameENG}");
                });

                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"บัตรประจำตัวประชาชน: {IDCard}");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"วัน/เดือน/ปี เกิด: {BirthDate}");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"อายุ: {Age} ปี");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"น้ำหนัก: {Weight} กก.");
                    row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ส่วนสูง: {height} ซม.");
                });

                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ที่อยู่(ปัจจุบัน): {CurrentAddress}");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"แขวง/ตำบล: {CurrentSubDistrict}");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"เขต/อำเภอ: {CurrentDistrict}");
                    row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"จังหวัด: {CurrentProvince}");
                    row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"รหัสไปรษณีย์: {ZipCode}");
                });

                col.Item().PaddingBottom(0).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทร: {Phone}");
                    row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"E-mail: {Email}");
                });

                // col.Item().Text($"ID Card No: {IdCardNo}");
                // col.Item().Text($"Address: {Address}");
                // col.Item().Text($"Province: {Province}, Post Code: {PostCode}");
            });

            page.Footer()
                .AlignCenter()
                .Text(x => x.CurrentPageNumber());
        });
    }
}
