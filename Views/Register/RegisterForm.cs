using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class PersonalDetailsForm : IDocument
    {
        public required string JobTitle { get; set; }
        public required string Salary { get; set; }
        public required string JobStartDate { get; set; }
        public required string FullNameTH { get; set; }
        public required string NickNameTH { get; set; }
        public required string FullNameENG { get; set; }
        public required string NickNameENG { get; set; }
        public required string IDCard { get; set; }
        public required string BirthDate { get; set; }
        public int Age { get; set; }
        public required string Weight { get; set; }
        public required string Height { get; set; }
        public required string CurrentAddress { get; set; }
        public required string CurrentSubDistrict { get; set; }
        public required string CurrentDistrict { get; set; }
        public required string CurrentProvince { get; set; }
        public required string ZipCode { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(20);
                page.MarginHorizontal(20);
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
                    col.Spacing(0);
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem(4).Border(0).BorderColor(Colors.Black).AlignRight().Padding(5).Text($"วันที่พร้อมเริ่มงาน: {JobStartDate}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem(5).Border(1).BorderRight(0).BorderColor(Colors.Black).Padding(5).Text($"ตำแหน่งที่ต้องการสมัคร: {JobTitle}").FontSize(8);
                        row.RelativeItem(3).Border(1).BorderColor(Colors.Black).Padding(5).Text($"อัตราเงินเดือนที่ต้องการ: {Salary} บาท").FontSize(8);
                    });
                    col.Item().Padding(5).Text("ข้อมูลส่วนตัว (Personal Details)").FontSize(11);
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อ-สกุล: {FullNameTH}").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อเล่น: {NickNameTH}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"Name-Surname: {FullNameENG}").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"Nick Name: {NickNameENG}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"บัตรประจำตัวประชาชน: {IDCard}").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"วัน/เดือน/ปี เกิด: {BirthDate}").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"อายุ: {Age} ปี").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"น้ำหนัก: {Weight} กก.").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ส่วนสูง: {Height} ซม.").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ที่อยู่(ปัจจุบัน): {CurrentAddress}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"แขวง/ตำบล: {CurrentSubDistrict}").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"เขต/อำเภอ: {CurrentDistrict}").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"จังหวัด: {CurrentProvince}").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"รหัสไปรษณีย์: {ZipCode}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"E-mail: {Email}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"สถานภาพสมรส: โสด").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"จำนวนบุตร: 0").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อคู่สมรส: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"อาชีพ: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"บริษัท: ").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderBottom(0).BorderColor(Colors.Black).Padding(5).Text($"E-mail: {Email}").FontSize(8);
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อบิดา: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"อายุ: ปี").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"อาชีพ: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทร: ").FontSize(8);
                        row.RelativeItem().BorderLeft(0).BorderTop(0).Row(innerRow =>
                        {
                            innerRow.RelativeItem().Text("มีชีวิต:").FontSize(8);
                        });
                        row.RelativeItem().BorderLeft(0).BorderRight(1).Row(innerRow =>
                        {
                            innerRow.RelativeItem().Text("ถึงแก่กรรม:").FontSize(8);
                        });
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ชื่อมารดา: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"อายุ: ปี").FontSize(8);
                        row.RelativeItem().Border(1).BorderRight(0).BorderLeft(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"อาชีพ: ").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderRight(0).BorderBottom(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทร: ").FontSize(8);
                        row.RelativeItem().BorderLeft(0).BorderTop(0).Row(innerRow =>
                        {
                            innerRow.RelativeItem().Text("มีชีวิต:").FontSize(8);
                        });
                        row.RelativeItem().BorderLeft(0).BorderRight(1).Row(innerRow =>
                        {
                            innerRow.RelativeItem().Text("ถึงแก่กรรม:").FontSize(8);
                        });
                    });
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderRight(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ท่านมีพี่-น้องจำนวน: คน").FontSize(8);
                        row.RelativeItem().Border(1).BorderLeft(0).BorderTop(0).BorderColor(Colors.Black).Padding(5).Text($"ท่านเป็นคนที่: (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(8);
                    });
                    col.Item().Padding(2).Text("").FontSize(11);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Padding(5).Text($"ชื่อ-นามสกุล(Name Surname)").FontSize(8).AlignCenter();
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Padding(5).Text($"อายุ(Age)").FontSize(8).AlignCenter();
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Padding(5).Text($"อาชีพ/ตำแหน่ง(Occupation)").FontSize(8).AlignCenter();
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Padding(5).Text($"บริษัท(Companys name)").FontSize(8).AlignCenter();
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Padding(5).Text($"เบอร์โทรติดต่อ(Tel.)").FontSize(8).AlignCenter();
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x => x.CurrentPageNumber());
            });
        }
    }
}