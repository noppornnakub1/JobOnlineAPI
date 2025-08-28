using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class PersonalDetailsForm : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        private readonly IDictionary<string, object> _form;
        public PersonalDetailsForm(IDictionary<string, object> form)
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
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DB Heavent"));
                page.Header()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Width(80)
                            .Image(Path.Combine("Views", "imagesform", "one_logo.png"));
                        col.Item().AlignCenter()
                            .PaddingTop(4).PaddingBottom(0)
                            .Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)")
                            .FontSize(10)
                            .Bold();
                        col.Item().AlignCenter()
                            .PaddingTop(2).PaddingBottom(0)
                            .Text("The ONE Enterprise Public Company Limited")
                            .FontSize(10);
                    });
                page.Content().Column(col =>
                {
                    col.Spacing(0);
                    col.Item().PaddingBottom(0).Row(row =>
                    {
                        row.RelativeItem(4).AlignRight().Padding(5).Text($"วันที่พร้อมเริ่มงาน: {_form["JobStartDate"] ?? ""}").FontSize(10);
                    });
                    col.Item().Border(1).BorderColor(Colors.Black).Column(innerCol =>
                    {
                        innerCol.Item().Row(row =>
                        {
                            row.RelativeItem(8)
                                .Padding(2)
                                .PaddingLeft(5)
                                .AlignMiddle()
                                .Text($"ตำแหน่งที่ต้องการสมัคร: {_form["JobTitle"] ?? ""}")
                                .FontSize(10);
                            row.RelativeItem(4)
                                .BorderLeft(1)
                                .BorderColor(Colors.Black)
                                .Padding(2)
                                .PaddingLeft(5)
                                .MinHeight(15)
                                .AlignMiddle()
                                .Text($"อัตราเงินเดือนที่ต้องการ: {_form["Salary"] ?? ""} บาท")
                                .FontSize(10);
                        });
                    });
                    col.Item().Padding(5).Text("ข้อมูลส่วนตัว (Personal Details)").FontSize(10);
                    col.Item().Border(1).BorderColor(Colors.Black).Padding(5).Column(innerCol =>
                    {
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อ-สกุล: {_form["FirstNameThai"] ?? ""}  {_form["LastNameThai"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"ชื่อเล่น: {_form["Nickname"] ?? ""}").FontSize(10);
                        });
                        innerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"Name-Surname: {_form["FirstNameEng"] ?? ""} {_form["LastNameEng"] ?? ""}").FontSize(10);
                            // row.RelativeItem().Padding(5).Text($"Nick Name: {_form.NickNameENG}").FontSize(10);
                        });
                        var birthDateText = _form["BirthDate"] is DateTime dt ? dt.ToString("dd/MM/yyyy") : "";
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem(4).Padding(5).Text($"บัตรประจำตัวประชาชน: {_form["CitizenID"] ?? ""}").FontSize(10);
                            row.RelativeItem(3).Padding(5).Text($"วัน/เดือน/ปี เกิด: {birthDateText}").FontSize(10);
                            row.RelativeItem(2).Padding(5).Text($"อายุ: {_form["Age"]} ปี").FontSize(10);
                            row.RelativeItem(2).Padding(5).Text($"น้ำหนัก: {_form["Weight"] ?? ""} กก.").FontSize(10);
                            row.RelativeItem(2).Padding(5).Text($"ส่วนสูง: {_form["Height"] ?? ""} ซม.").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ที่อยู่(ปัจจุบัน): {_form["CurrentAddress"] ?? ""}").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"แขวง/ตำบล: {_form["CurrentSubDistrict"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"เขต/อำเภอ: {_form["CurrentDistrict"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"จังหวัด: {_form["CurrentProvince"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"รหัสไปรษณีย์: {_form["CurrentPostalCode"] ?? ""}").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: {_form["MobilePhone"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"E-mail: {_form["Email"] ?? ""}").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"สถานภาพสมรส: {_form["MaritalStatus"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"จำนวนบุตร: {Convert.ToInt32(_form["MaleChildren"] ?? 0) + Convert.ToInt32(_form["FemaleChildren"] ?? 0)}").FontSize(10);
                            // .Text($"จำนวนบุตร: {_form.MaleChildren + _form.FemaleChildren}").FontSize(10);

                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อคู่สมรส: {_form["SpouseFullName"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: {_form["SpouseOccupation"] ?? ""}").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"ประเภทธุรกิจ: {_form["SpouseCompanyType"] ?? ""}").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"สถานที่ทำงาน: {_form["SpouseCompanyAddress"] ?? ""}").FontSize(10);
                            // row.RelativeItem().Padding(5).Text($"เบอร์โทร: {Phone}").FontSize(10);
                            // row.RelativeItem().Padding(5).Text($"E-mail: {Email}").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อบิดา: ").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"อายุ:  ปี").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: ").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: ").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ชื่อมารดา: ").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"อายุ:  ปี").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"อาชีพ: ").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"เบอร์โทร: ").FontSize(10);
                        });
                        innerCol.Item().PaddingBottom(0).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"ท่านมีพี่-น้องจำนวน: คน").FontSize(10);
                            row.RelativeItem().Padding(5).Text($"ท่านเป็นคนที่: (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(10);
                        });
                    });
                    var educationList = new List<EducationsDto>();
                    if (_form["EducationList"] != null && _form["EducationList"] != DBNull.Value)
                    {
                        var options = new JsonSerializerOptions
                        {
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };
                        educationList = JsonSerializer.Deserialize<List<EducationsDto>>(
                            _form["EducationList"].ToString() ?? "[]", options
                        ) ?? new List<EducationsDto>();
                    }
                    if (educationList.Count == 0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            educationList.Add(new EducationsDto());
                        }
                    }
                    col.Item().Padding(5).Text("ข้อมูลประวัติการศึกษา (Educational Details)").FontSize(10);
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
                            .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ชื่อสถานศึกษา\n(Name of place)").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("จังหวัด/ประเทศ\n(Province/Country)").FontSize(10);
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ปีการศึกษา\n(Graduated year)").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("วุฒิการศึกษา / สาขา\n(Education Level / Major)").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("GPA").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี\n(From)").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี\n(To)").FontSize(10);
                        foreach (var edu in educationList)
                        {
                            table.Cell().Border(1).Padding(3).Text(edu.EducationLevel ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(edu.InstitutionName ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(edu.Province ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.StartYear?.ToString() ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.EndYear?.ToString() ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(edu.Major ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(edu.GPA ?? 0).FontSize(10);
                        }
                    });

                    var workList = new List<WorkExperiencesDto>();
                    if (_form["WorkExperienceList"] != null && _form["WorkExperienceList"] != DBNull.Value)
                    {
                        workList = JsonSerializer.Deserialize<List<WorkExperiencesDto>>(
                            _form["WorkExperienceList"].ToString() ?? "[]"
                        ) ?? new List<WorkExperiencesDto>();
                    }

                    // ถ้าไม่มีข้อมูล → เติม Default 4 row
                    if (workList.Count == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            workList.Add(new WorkExperiencesDto());
                        }
                    }
                    col.Item().Padding(5).Text("ข้อมูลประวัติการทำงาน (Work Experiences)").FontSize(10);
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Start
                            columns.RelativeColumn(2); // End
                            columns.RelativeColumn(3); // Company
                            columns.RelativeColumn(4); // Position
                            columns.RelativeColumn(3); // Responsibilities
                            columns.RelativeColumn(4); // Reason
                            columns.RelativeColumn(2); // Salary
                        });
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ระยะเวลา\nPeriod").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("บริษัท\nCompany's Name").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ตำแหน่ง\nPosition").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ลักษณะงานโดยสังเขป\nJob descriptions").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("เหตุผลที่ลาออก\nReasion for leaving").FontSize(10);
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("เงินเดือนสุดท้าย").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี(From)\nMM/YY").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี(To)\nMM/YY").FontSize(10);

                        foreach (var work in workList)
                        {
                            table.Cell().Border(1).Padding(3).Text(work.StartDate ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.EndDate ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.CompanyName ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.Position ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.Responsibilities ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.ReasonForLeaving ?? "").FontSize(10);
                            table.Cell().Border(1).Padding(3).Text(work.Salary ?? "").FontSize(10);
                        }
                    });

                });
                page.Footer().AlignRight().Column(col =>
                {
                    col.Item().Padding(5).Text("ลงชื่อผู้สมัคร ............................................").FontSize(10);
                    col.Item().Padding(5).Text("Signature (..........................................)").FontSize(10);
                    col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                });
            });
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(20);
                page.MarginHorizontal(20);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DB Heavent"));
                page.Header()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Width(80)
                            .Image(Path.Combine("Views", "imagesform", "one_logo.png"));
                        col.Item().AlignCenter()
                            .PaddingTop(4).PaddingBottom(0)
                            .Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)")
                            .FontSize(10)
                            .Bold();
                        col.Item().AlignCenter()
                            .PaddingTop(2).PaddingBottom(0)
                            .Text("The ONE Enterprise Public Company Limited")
                            .FontSize(10);
                    });
                page.Content().Column(col =>
                {
                    var skills = new List<SkillsDto>();

                    if (_form["SkillsList"] != null && _form["SkillsList"] != DBNull.Value)
                    {
                        var options = new JsonSerializerOptions
                        {
                            NumberHandling = JsonNumberHandling.AllowReadingFromString
                        };

                        skills = JsonSerializer.Deserialize<List<SkillsDto>>(
                            _form["SkillsList"]?.ToString() ?? "[]", options
                        ) ?? new List<SkillsDto>();
                    }

                    if (skills.Count == 0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            skills.Add(new SkillsDto
                            {
                                SkillType = "",
                                SkillDescription = "",
                                SkillScore = null
                            });
                        }
                    }
                    col.Item().Padding(5).Text("ความสามารถพิเศษ").FontSize(10);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Black).Column(col1 =>
                        {
                            col1.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text("ภาษาต่างประเทศ / Skills").FontSize(10);

                            foreach (var skill in skills)
                            {
                                if (skill.SkillType == "TOEIC" || skill.SkillType == "TOEFL" || skill.SkillType == "IELTS")
                                {
                                    col1.Item().Padding(5)
                                    .Text($"{skill.SkillType ?? ""}: {skill.SkillScore ?? 0} คะแนน")
                                    .FontSize(10);
                                }
                            }
                        });

                        row.RelativeItem().PaddingLeft(5).Column(col2 =>
                        {

                            foreach (var skill in skills)
                            {
                                if (skill.SkillType == "ComSkill" || skill.SkillType == "genius")
                                {
                                    col2.Item().Border(1).BorderColor(Colors.Black).Column(box =>
                                    {
                                        // Header
                                        box.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text(skill.SkillType == "ComSkill" 
                                                ? "ความรู้ทางคอมพิวเตอร์ (Computer Skills)" 
                                                : "ความรู้หรือทักษะอื่น ๆ (Other Skills)")
                                            .FontSize(10);

                                        // ถ้ามีข้อความ → แตกเป็นหลายบรรทัด
                                        var desc = (skill.SkillDescription ?? "").Split('\n');
                                        foreach (var line in desc)
                                        {
                                            box.Item()
                                                .PaddingHorizontal(5).Height(20)
                                                .BorderBottom(0.5f).BorderColor(Colors.Black)
                                                .Text(line).FontSize(10);     
                                        }

                                        // ถ้าไม่มี หรือบรรทัดน้อยกว่า 3 → เติม blank row
                                        int filled = desc.Length;
                                        for (int i = filled; i < 3; i++)
                                        {
                                            box.Item().PaddingHorizontal(5).Height(20)
                                                .BorderBottom(0.5f).BorderColor(Colors.Black);
                                        }
                                    });
                                }
                            }
                        });
                    });

                    col.Item().PaddingTop(8).Text("ข้าพเจ้ายินยอมให้ตรวจสอบประวัติจากผู้ว่าจ้างเดิมถึงปัจจุบัน").FontSize(10);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status1"]?.ToString()?.ToLower() == "yes" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ยินยอม (Yes)").FontSize(10);

                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status1"]?.ToString()?.ToLower() == "no" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่ยินยอม (No)").FontSize(10);
                    });

                    col.Item().PaddingTop(8).Text("ข้าพเจ้าเคยสมัครงานกับบริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(10);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status2"]?.ToString()?.ToLower() == "yes" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("เคย (Yes)").FontSize(10);

                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status2"]?.ToString()?.ToLower() == "no" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่เคย (No)").FontSize(10);
                    });


                    col.Item().PaddingTop(8).Text("ข้าพเจ้าเคยป่วยหนักหรือต้องพักรักษาตัวอยู่ในสถานพยาบาล").FontSize(10);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status3"]?.ToString()?.ToLower() == "yes" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("เคย (Yes)").FontSize(10);

                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status3"]?.ToString()?.ToLower() == "no" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่เคย (No)").FontSize(10);
                    });

                    col.Item().PaddingTop(8).Text("ข้าพเจ้ามีโรคประจำตัว").FontSize(10);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status4"]?.ToString()?.ToLower() == "no" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("ไม่มี (No)").FontSize(10);

                        row.ConstantItem(12).MinHeight(12)
                            .Border(1).BorderColor(Colors.Black)
                            .AlignCenter().AlignMiddle()
                            .Text(_form["Marital_Status4"]?.ToString()?.ToLower() != "no" ? "✓" : "")
                            .FontSize(8);
                        row.ConstantItem(100).PaddingLeft(5)
                            .Text("มี (Yes)").FontSize(10);
                            
                        row.ConstantItem(100)
                        .Text($"{( _form["Marital_Status4"]?.ToString()?.ToLower() != "no" ? $"โรคประจำตัว: {_form["Marital_Status4"]}" : "")}")
                        .FontSize(10);   
                    });

                    col.Item().PaddingTop(8).Text("บุคคลที่ติดต่อในกรณีเร่งด่วน").FontSize(10);
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                        });

                        // Header
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ชื่อ-สกุล\n(Name-Surname)").FontSize(10);

                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ความสัมพันธ์\n(Relation)").FontSize(10);

                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("โทรศัพท์\n(Tel.)").FontSize(10);

                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ที่อยู่\n(Address)").FontSize(10);

                        // Data Row
                        table.Cell().Border(1).Padding(3)
                            .Text(_form["EmergencyContactName"]?.ToString() ?? "").FontSize(10);

                        table.Cell().Border(1).Padding(3)
                            .Text(_form["EmergencyContactRelationship"]?.ToString() ?? "").FontSize(10);

                        table.Cell().Border(1).Padding(3)
                            .Text(_form["EmergencyContactPhone"]?.ToString() ?? "").FontSize(10);

                        table.Cell().Border(1).Padding(3)
                            .Text(_form["EmergencyContactAddress"]?.ToString() ?? "")
                            .FontSize(10)
                            .WrapAnywhere();
                    });
                });
                page.Footer().AlignRight().Column(col =>
                {
                    col.Item().Padding(5).Text("ลงชื่อผู้สมัคร ............................................").FontSize(10);
                    col.Item().Padding(5).Text("Signature (..........................................)").FontSize(10);
                    col.Item().AlignCenter().Padding(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                });
            });
        }
    }
    public class EducationsDto
    {
        public string? EducationLevel { get; set; }
        public string? InstitutionName { get; set; }
        public string? Province { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? Major { get; set; }
        public decimal? GPA { get; set; } 
    }
    public class WorkExperiencesDto
    {
        public string? CompanyName { get; set; }
        public string? Position { get; set; }
        public string? Responsibilities { get; set; }
        public string? ReasonForLeaving { get; set; }
        public string? Salary { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
    public class SkillsDto
    {
        public string? SkillType { get; set; }
        public string? SkillDescription { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? SkillScore { get; set; } 
    }
}