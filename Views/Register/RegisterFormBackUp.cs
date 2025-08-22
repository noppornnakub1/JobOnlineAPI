using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class PersonalDetailsForms : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        private readonly dynamic _form;
        public PersonalDetailsForms(dynamic form)
        {
            _form = form;
        }

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
                        row.RelativeItem(4).AlignRight().Padding(5).Text($"วันที่พร้อมเริ่มงาน: {_form.BirthDate}").FontSize(8);
                    });
                    col.Item().Border(1).BorderColor(Colors.Black).Column(innerCol =>
                    {
                        innerCol.Item().Row(row =>
                        {
                            row.RelativeItem(8)
                                .Padding(2)
                                .PaddingLeft(5)
                                .AlignMiddle()
                                .Text($"ตำแหน่งที่ต้องการสมัคร: {_form.JobTitle}")
                                .FontSize(8);
                            row.RelativeItem(4)
                                .BorderLeft(1)
                                .BorderColor(Colors.Black)
                                .Padding(2)
                                .PaddingLeft(5)
                                .MinHeight(15)
                                .AlignMiddle()
                                .Text($"อัตราเงินเดือนที่ต้องการ: {_form.Salary} บาท")
                                .FontSize(8);
                        });
                    });
                    col.Item().Padding(5).Text("ข้อมูลส่วนตัว (Personal Details)").FontSize(9);
                    col.Item().Border(1).BorderColor(Colors.Black).Padding(5).Column(innerCol =>
                    {
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อ-สกุล: {_form.FirstNameThai}  {_form.LastNameThai}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"ชื่อเล่น: {_form.Nickname}").FontSize(8);
                        });
                        innerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"Name-Surname: {_form.FirstNameEng} {_form.LastNameEng}").FontSize(8);
                            // row.RelativeItem().Padding(5).Text($"Nick Name: {_form.NickNameENG}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem(4).Padding(5).Text($"บัตรประจำตัวประชาชน: {_form.CitizenID}").FontSize(8);
                            row.RelativeItem(3).Padding(5).Text($"วัน/เดือน/ปี เกิด: {_form.BirthDate}").FontSize(8);
                            row.RelativeItem(2).Padding(5).Text($"อายุ: 0 ปี").FontSize(8);
                            row.RelativeItem(2).Padding(5).Text($"น้ำหนัก: {_form.Weight} กก.").FontSize(8);
                            row.RelativeItem(2).Padding(5).Text($"ส่วนสูง: {_form.Height} ซม.").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ที่อยู่(ปัจจุบัน): {_form.CurrentAddress}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"แขวง/ตำบล: {_form.CurrentSubDistrict}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"เขต/อำเภอ: {_form.CurrentDistrict}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"จังหวัด: {_form.CurrentProvince}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"รหัสไปรษณีย์: {_form.CurrentPostalCode}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: {_form.MobilePhone}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"E-mail: {_form.Email}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"สถานภาพสมรส: {_form.MaritalStatus}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"จำนวนบุตร: {_form.MaleChildren + _form.FemaleChildren}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อคู่สมรส: {_form.SpouseFullName}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: {_form.SpouseOccupation}").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"ประเภทธุรกิจ: {_form.SpouseCompanyType}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"สถานที่ทำงาน: {_form.SpouseCompanyAddress}").FontSize(8);
                            // row.RelativeItem().Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(8);
                            // row.RelativeItem().Padding(5).Text($"E-mail: {Email}").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อบิดา: ................................").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"อายุ: ........ ปี").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: ................................").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: ................................").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อมารดา: ................................").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"อายุ: ........ ปี").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: ................................").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: ................................").FontSize(8);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ท่านมีพี่-น้องจำนวน: ........ คน").FontSize(8);
                            row.RelativeItem().Padding(5).Text($"ท่านเป็นคนที่: (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(8);
                        });
                    });
                    // col.Item().Padding(5).Text("ข้อมูลประวัติการศึกษา (Educational Details)").FontSize(9);
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(8);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ชื่อสถานศึกษา\n(Name of place)").FontSize(8);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("จังหวัด/ประเทศ\n(Province/Country)").FontSize(8);
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ปีการศึกษา\n(Graduated year)").FontSize(8);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(8);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("GPA").FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี\n(From)").FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี\n(To)").FontSize(8);
                        void AddRow(string level)
                        {
                            table.Cell().Border(1).Padding(3).Text(level).FontSize(8);
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
                        }
                        AddRow("มัธยมศึกษา (Secondary)");
                        AddRow("ประกาศนียบัตรวิชาชีพ (Vocational)");
                        AddRow("ปริญญาตรี (Bachelor)");
                        AddRow("ปริญญาโท (Master)");
                        AddRow("อื่น ๆ (Other)");
                    });
                    col.Item().Padding(5).Text("ข้อมูลประวัติการทำงาน (Work Experiences)").FontSize(9);
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });
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
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี(From)\nMM/YY").FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี(To)\nMM/YY").FontSize(8);
                        void AddRow(string level)
                        {
                            table.Cell().Border(1).Padding(3).Text("");
                            table.Cell().Border(1).Padding(3).Text("");
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
                page.Footer().AlignRight().Column(col =>
                {
                    col.Item().Padding(5).Text("ลงชื่อผู้สมัคร ............................................").FontSize(9);
                    col.Item().Padding(5).Text("Signature (..........................................)").FontSize(9);
                    col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                });
            });
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
                    col.Item().Padding(5).Text("ความสามารถพิเศษ").FontSize(9);
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

                    col.Item().PaddingTop(8).Text("ข้าพเจ้ายินยอมให้ตรวจสอบประวัติจากผู้ว่าจ้างเดิมถึงปัจจุบัน").FontSize(9);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ยินยอม (Yes)").FontSize(8);

                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black).PaddingLeft(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่ยินยอม (No)").FontSize(8);
                    });

                    col.Item().PaddingTop(8).Text("ข้าพเจ้าเคยสมัครงานกับบริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(9);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("เคย (Yes)").FontSize(8);

                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black).PaddingLeft(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่เคย (No)").FontSize(8);
                    });


                    col.Item().PaddingTop(8).Text("ข้าพเจ้าเคยป่วยหนักหรือต้องพักรักษาตัวอยู่ในสถานพยาบาล").FontSize(9);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("เคย (Yes)").FontSize(8);

                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black).PaddingLeft(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่เคย (No)").FontSize(8);
                    });

                    col.Item().PaddingTop(8).Text("ข้าพเจ้ามีโรคประจำตัว").FontSize(9);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่มี (No)").FontSize(8);

                        row.ConstantItem(8).Height(8)
                            .Border(1).BorderColor(Colors.Black).PaddingLeft(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("มี (Yes) .....................................").FontSize(8);
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
                    col.Item().Padding(5).Text("ลงชื่อผู้สมัคร ............................................").FontSize(9);
                    col.Item().Padding(5).Text("Signature (..........................................)").FontSize(9);
                    col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                });
            });
        }
    }
}