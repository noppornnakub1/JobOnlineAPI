using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;
namespace JobOnlineAPI.Views.Register
{
    public class PersonalDetailsV2Form : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        private readonly IDictionary<string, object> _form;
        public PersonalDetailsV2Form(IDictionary<string, object> form)
        {
            _form = form;
        }
        [Obsolete("This method is obsolete. Please use the updated IDocument interface implementation or a newer document generation approach.")]
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.MarginVertical(8);
                            page.MarginHorizontal(8);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DB Heavent"));
                            page.Header().Column(headerCol =>
                                {
                                    headerCol.Item().ShowOnce().Element(ComposeFirstPageHeader);// ✅ Header หน้าแรก (มีกรอบรูป)
                                    headerCol.Item().SkipOnce().Element(ComposeOtherPageHeader); // ✅ Header ทุกหน้าถัดไป (ไม่มีกรอบรูป)
                                });
                            page.Content().PaddingTop(5).Column(col =>
                                {
                                    col.Item().Border(1).BorderColor(Colors.Black).Padding(3).Column(innerRow =>
                                            {
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text("ข้อมูลส่วนตัว (Personal Details)").Bold().FontSize(13);
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(7).Padding(3).Text(
                                            text =>
                                                                                {
                                                                                    text.Span("ชื่อ-สกุล: ").FontSize(12).Bold();
                                                                                    text.Span($"{_form["FirstNameThai"] ?? ""} {_form["LastNameThai"] ?? ""}").FontSize(12);
                                                                                }
                                                                            );
                                                            col.RelativeItem(5).AlignLeft().Padding(3).Text(
                                            text =>
                                                                                {
                                                                                    text.Span("ชื่อเล่น: ").FontSize(12).Bold();
                                                                                    text.Span($"{_form["Nickname"] ?? ""}").FontSize(12);
                                                                                }
                                                                            );
                                                            col.RelativeItem(6);
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                                {
                                                                                    text.Span("Name-Surname: ").FontSize(12).Bold();
                                                                                    text.Span($"{_form["FirstNameEng"] ?? ""} {_form["LastNameEng"] ?? ""}").FontSize(12);
                                                                                }
                                                                            );
                                                        });
                                                var birthDateText = _form["BirthDate"] is DateTime dt ? dt.ToString("dd/MM/yyyy") : "";
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(6).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("วัน/เดือน/ปี เกิด [Date Of Birth]: ").FontSize(12).Bold();
                                                                                text.Span($"{birthDateText}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อายุ [Age]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Age"]} ปี").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("น้ำหนัก [Weight]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Weight"] ?? ""} กก.").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ส่วนสูง [Height]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Height"] ?? ""} ซม.").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(7).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("บัตรประจำตัวประชาชน[CitizenID]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["CitizenID"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(5).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ออกให้ ณ [Issued by]: ").FontSize(12).Bold();
                                                                                text.Span($"กรุงเทพมหานคร").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(6).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("บัตรหมดอายุวันที่ [Expiry date]: ").FontSize(12).Bold();
                                                                                text.Span($"dd-MM-yyyy").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                // ---------------------------------------------- ที่อยู่ตามทะเบียนบ้าน ----------------------------------------------
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ที่อยู่ตามทะเบียนบ้าน [Registeres Address]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["CurrentAddress"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("แขวง/ตำบล [Tumbol]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SubDistrictNameThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เขต/อำเภอ [District]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["DistrictNameThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("จังหวัด [Province]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["ProvinceNameThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("รหัสไปรษณีย์ [PostalCode]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["CurrentPostalCode"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                // ---------------------------------------------- ที่อยู่ปัจจุบัน ----------------------------------------------
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ที่อยู่ปัจจุบัน [Current Address]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["CurrentAddress"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("แขวง/ตำบล [Tumbol]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["RegisteredSubDistrictThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เขต/อำเภอ [District]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["RegisteredDistrictThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("จังหวัด [Province]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["RegisteredProvinceThai"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("รหัสไปรษณีย์ [Postal Code]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["RegisteredPostalCode"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                // ---------------------------------------------- ข้อมูลติดต่อ ----------------------------------------------
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(4).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["MobilePhone"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(4).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("Email: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Email"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                            }); // Close ข้อมูลส่วนตัว
                                                // ---------------------------------------------- ข้อมูลอื่นๆ ----------------------------------------------
                                    col.Item().PaddingTop(0).Border(1).BorderColor(Colors.Black).Column(innerRow =>
                                            {
                                                //----------------------- สถานภาพทางทหาร -----------------------
                                                innerRow.Item().PaddingTop(2).PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(2).Padding(3).Column(col =>
                                                                        {
                                                                            col.Item().Text("สถานภาพทางทหาร: ").FontSize(12).Bold();
                                                                            col.Item().Text("Military Service").FontSize(12).Bold();
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["Marital_Status1"]?.ToString() ?? "", "completed", "ผ่านการเกณฑ์ทหาร", "Completed");
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["Marital_Status1"]?.ToString() ?? "", "no completed", "ยังไม่ได้เกณฑ์ทหาร", "No Completed");
                                                                        });
                                                            col.RelativeItem(6).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["Marital_Status1"]?.ToString() ?? "", "exemted", "ได้รับการยกเว้น", "Exemted,Please specific");
                                                                            row.RelativeItem().Text(
                                                        text =>
                                                                                            {
                                                                                                text.Span("เนื่องจาก: ").FontSize(12).Bold();
                                                                                                text.Span($"{_form["ReasonMilitary"] ?? "............................."}").FontSize(12);
                                                                                            });
                                                                        });
                                                        });
                                                //----------------------- สถานภาพสมรส -----------------------
                                                innerRow.Item().PaddingTop(2).PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(2).Padding(3).Column(col =>
                                                                        {
                                                                            col.Item().Text("สถานภาพสมรส: ").FontSize(12).Bold();
                                                                            col.Item().Text("Marital Status").FontSize(12).Bold();
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["MaritalStatus"]?.ToString() ?? "", "single", "โสด", "Single");
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["MaritalStatus"]?.ToString() ?? "", "married", "สมรส", "Married");
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["MaritalStatus"]?.ToString() ?? "", "divorced", "หย่า", "Divorced");
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["MaritalStatus"]?.ToString() ?? "", "widowed", "หม้าย", "Widowed");
                                                                        });
                                                            col.RelativeItem(4).PaddingLeft(10).PaddingTop(4).Row(row =>
                                                                        {
                                                                            row.RelativeItem().PaddingLeft(5)
                                                        .Text(
                                                        text =>
                                                                                            {
                                                                                                text.Span("จำนวนบุตร: ").FontSize(12).Bold();
                                                                                                text.Span($"{Convert.ToInt32(_form["MaleChildren"] ?? 0) + Convert.ToInt32(_form["FemaleChildren"] ?? 0)} คน").FontSize(12);
                                                                                                text.Span("\n No. of Children: ").FontSize(12).Bold();
                                                                                            }
                                                                                        );
                                                                        });
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(7).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ชื่อคู่สมรส [Spouse's Name]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseFullName"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "alive", "มีชีวิต", "");
                                                                        });
                                                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "deceased", "ถึงแก่กรรม", "");
                                                                        });
                                                            col.RelativeItem(5).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อาชีพ [Occupation]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseOccupation"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("สถานที่ทำงาน [Workplace]: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseCompanyAddress"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(4).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["MobilePhone"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(4).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("Email: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Email"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                            });
                                    // ---------------------------------------------- ข้อมูลครอบครัว ----------------------------------------------
                                    col.Item().PaddingTop(5).Column(innerRow =>
                                            {
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(5).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ชื่อ-สกุล บิดา: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseFullName"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อายุ: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Age"]} ปี").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อาชีพ: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseOccupation"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["MobilePhone"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).AlignRight().PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "alive", "มีชีวิต", "");
                                                                        });
                                                            col.RelativeItem(2).AlignRight().PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "deceased", "ถึงแก่กรรม", "");
                                                                        });
                                                        });
                                                innerRow.Item().PaddingLeft(5).Row(col =>
                                                        {
                                                            col.RelativeItem(5).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ชื่อ-สกุล มารดา: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseFullName"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อายุ: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["Age"]} ปี").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("อาชีพ: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["SpouseOccupation"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(3).Padding(3).Text(
                                            text =>
                                                                            {
                                                                                text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["MobilePhone"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                            col.RelativeItem(2).AlignRight().PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "alive", "มีชีวิต", "");
                                                                        });
                                                            col.RelativeItem(2).AlignRight().PaddingTop(4).Row(row =>
                                                                        {
                                                                            RenderCheckBox(row, _form["SpouseStatus"]?.ToString() ?? "", "deceased", "ถึงแก่กรรม", "");
                                                                        });
                                                        });
                                                innerRow.Item().PaddingTop(1).PaddingBottom(1).Row(col =>
                                                        {
                                                            col.RelativeItem().PaddingTop(5).Text("ท่านมีพี่-น้องจำนวน xx คน ท่านเป็นคนที่ xx (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(12).Bold();
                                                        });
                                                innerRow.Item().PaddingRight(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Border(1).BorderColor(Colors.Black).Table(table =>
                                                                        {
                                                                            table.ColumnsDefinition(columns =>
                                                                                        {
                                                                                            columns.RelativeColumn(3);
                                                                                            columns.RelativeColumn(1);
                                                                                            columns.RelativeColumn(3);
                                                                                            columns.RelativeColumn(4);
                                                                                            columns.RelativeColumn(2);
                                                                                        });
                                                                            // Header
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("ชื่อ-สกุล[Name-Surname]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("อายุ[Age]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle().Column(col =>
                                                                                            {
                                                                                                col.Item().Text("อาชีพ/ตำแหน่ง").FontSize(12).Bold();
                                                                                                col.Item().Text("[Occupation]").FontSize(12).Bold();
                                                                                            });
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("บริษัท[Company's Name]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("เบอร์โทรศัพท์[Phone Number]").FontSize(12).Bold();
                                                                            // Data Row
                                                                            for (int i = 0; i < 3; i++)
                                                                            {
                                                                                table.Cell().Border(1).Padding(3)
                                                            .Text(_form["EmergencyContactNams"]?.ToString() ?? "นาย การิน อริยวัฒน์").FontSize(12).AlignCenter();
                                                                                table.Cell().Border(1).Padding(3)
                                                            .Text(_form["EmergencyContactRelationshp"]?.ToString() ?? "29 ปี").FontSize(12).AlignCenter();
                                                                                table.Cell().Border(1).Padding(3)
                                                            .Text(_form["EmergencyContactPhon"]?.ToString() ?? "เลขา").FontSize(12).AlignCenter();
                                                                                table.Cell().Border(1).Padding(3)
                                                            .Text(_form["EmergencyContactAddress"]?.ToString() ?? "SC Tech.co")
                                                            .FontSize(12).AlignCenter()
                                                            .WrapAnywhere();
                                                                                table.Cell().Border(1).Padding(3)
                                                            .Text(_form["MobilePhone"]?.ToString() ?? "xxxxxxxxxx")
                                                            .FontSize(12).AlignCenter()
                                                            .WrapAnywhere();
                                                                            }
                                                                        });
                                                        });
                                                innerRow.Item().PaddingTop(1).PaddingBottom(1).Row(col =>
                                                        {
                                                            col.RelativeItem().PaddingTop(5).Text("บุคคลที่ติดต่อในกรณีเร่งด่วน").FontSize(12).Bold();
                                                        });
                                                innerRow.Item().PaddingRight(5).PaddingBottom(5).Row(col =>
                                                        {
                                                            col.RelativeItem().Border(1).BorderColor(Colors.Black).Table(table =>
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
                                                        .Text("ชื่อ-สกุล[Name-Surname]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("ความสัมพันธ์[Relation]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("โทรศัพท์[Phone Number]").FontSize(12).Bold();
                                                                            table.Cell().Border(1).BorderColor(Colors.Black)
                                                        .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                                        .Text("ที่อยู่[Address]").FontSize(12).Bold();
                                                                            // Data Row
                                                                            table.Cell().Border(1).Padding(3).AlignCenter()
                                                        .Text(_form["EmergencyContactName"]?.ToString() ?? "").FontSize(12);
                                                                            table.Cell().Border(1).Padding(3).AlignCenter()
                                                        .Text(_form["EmergencyContactRelationship"]?.ToString() ?? "").FontSize(12);
                                                                            table.Cell().Border(1).Padding(3).AlignCenter()
                                                        .Text(_form["EmergencyContactPhone"]?.ToString() ?? "").FontSize(12);
                                                                            table.Cell().Border(1).Padding(3).AlignCenter()
                                                        .Text(_form["EmergencyContactAddress"]?.ToString() ?? "")
                                                        .FontSize(12)
                                                        .WrapAnywhere();
                                                                        });
                                                        });
                                            });
                                }); // Close Content Page 1
                        });
        }
        private static void RenderCheckBox(RowDescriptor row, string formValue, string expectedValue, string label, string sublabel)
        {
            // ✅ กล่องติ๊ก
            row.ConstantItem(15).Height(15).CornerRadius(1)
            .Border(1).BorderColor(Colors.Black)
            .AlignCenter().AlignMiddle()
            .Text(string.Equals(formValue, expectedValue, StringComparison.OrdinalIgnoreCase) ? "✓" : "")
            .FontSize(9).Bold();
            row.RelativeItem().PaddingLeft(5).Column(col =>
                        {
                            col.Item().Text(label).FontSize(12).Bold();
                            if (!string.IsNullOrWhiteSpace(sublabel))
                            {
                                col.Item().Text(sublabel).FontSize(12);
                            }
                        });
        }
        private void ComposeFirstPageHeader(IContainer container)
        {
            container.Row(row =>
                        {
                            // ✅ ซ้าย: โลโก้ + ข้อความบริษัท (กลางหน้า)
                            row.RelativeItem(16).Column(col =>
                                {
                                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");
                                    // โลโก้ตรงกลาง
                                    col.Item().AlignCenter().PaddingLeft(50).Width(80).Image(imagePath).FitWidth();
                                    col.Item().AlignCenter().PaddingLeft(50).Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(12).Bold();
                                    col.Item().AlignCenter().PaddingLeft(50).Text("The ONE Enterprise Public Company Limited").FontSize(12);
                                    // กรอบเต็มความกว้าง (ยาวเหมือนภาพ 2)
                                    col.Item().PaddingTop(5);
                                    col.Item().Border(1).BorderColor(Colors.Black).Padding(8).Row(innerRow =>
                                            {
                                                // ชื่อบริษัท
                                                innerRow.RelativeItem(6).AlignCenter().Column(left =>
                                                        {
                                                            left.Item().Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(12).Bold();
                                                            left.Item().Text("The ONE Enterprise Public Company Limited").FontSize(12).Bold();
                                                        });
                                                // วันที่พร้อมเริ่มงาน + อัตราเงินเดือนที่ต้องการ
                                                innerRow.RelativeItem(6).AlignCenter().Column(right =>
                                                        {
                                                            var jobStartDateText = _form["JobStartDate"] is DateTime dt ? dt.ToString("dd/MM/yyyy") : "";
                                                            right.Item().Text(t =>
                                                                        {
                                                                            t.Span("วันที่พร้อมเริ่มงาน: ").Bold().FontSize(12);
                                                                            t.Span(jobStartDateText).FontSize(12);
                                                                        });
                                                            right.Item().Text(t =>
                                                                        {
                                                                            var salary = decimal.TryParse(_form["Salary"]?.ToString(), out var s) ? s.ToString("N0") : "";
                                                                            t.Span("อัตราเงินเดือนที่ต้องการ: ").Bold().FontSize(12);
                                                                            t.Span($"{salary} บาท").FontSize(12);
                                                                        });
                                                        });
                                            });
                                    col.Item().PaddingTop(5);
                                    col.Item().Border(1).BorderColor(Colors.Black).Padding(8).Row(innerRow =>
                                            {
                                                // ตำแหน่งที่ต้องการสมัคร
                                                innerRow.RelativeItem(6).Column(left =>
                                                        {
                                                            left.Item().PaddingLeft(10).Text(
                                            text =>
                                                                            {
                                                                                text.Span("ตำแหน่งที่ต้องการสมัคร: ").FontSize(12).Bold();
                                                                                text.Span($"{_form["JobTitle"] ?? ""}").FontSize(12);
                                                                            }
                                                                        );
                                                        });
                                            });
                                });
                            // กรอบรูปถ่าย
                            row.RelativeItem(4).AlignTop().AlignRight().PaddingTop(10).Column(col =>
                                {
                                    col.Item().Width(110).Height(150) // ~ 1.5 x 2 นิ้ว
                        .Border(1).BorderColor(Colors.Black)
                        .AlignCenter().AlignMiddle()
                        .Text("ติดรูปถ่าย\n1.5 x 2 นิ้ว").FontSize(12);
                                });
                        });
        }
        private void ComposeOtherPageHeader(IContainer container)
        {
            container.Column(col =>
                        {
                            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");
                            col.Item().AlignCenter().Width(80).Image(imagePath).FitWidth();
                            col.Item().AlignCenter().Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(12).Bold();
                            col.Item().AlignCenter().Text("The ONE Enterprise Public Company Limited").FontSize(12);
                        });
        }
    }
    public class EducationsV2Dto
    {
        public string? EducationLevel { get; set; }
        public string? InstitutionName { get; set; }
        public string? Province { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? Major { get; set; }
        public decimal? GPA { get; set; }
    }
    public class WorkExperiencesV2Dto
    {
        public string? CompanyName { get; set; }
        public string? Position { get; set; }
        public string? Responsibilities { get; set; }
        public string? ReasonForLeaving { get; set; }
        public string? Salary { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
    public class SkillsV2Dto
    {
        public string? SkillType { get; set; }
        public string? SkillDescription { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? SkillScore { get; set; }
    }
}