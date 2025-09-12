using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace JobOnlineAPI.Views.Register
{
    public class PersonalDetailsV3Form : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        private readonly IDictionary<string, object> _form;
        public PersonalDetailsV3Form(IDictionary<string, object> form)
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
                page.Content().Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Black).Padding(3).Column(innerRow =>
                    {
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem().Padding(3).Text("ข้อมูลส่วนตัว (Personal Details)").Bold().FontSize(13);
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(4).Padding(3).Text(
                                    text =>
                                    {
                                        text.Span("ชื่อ-สกุล[TH]: ").FontSize(12).Bold();
                                        text.Span($"{_form["FirstNameThai"] ?? ""} {_form["LastNameThai"] ?? ""}").FontSize(12);
                                    }
                                );
                            col.RelativeItem(4).AlignLeft().Padding(3).Text(
                                    text =>
                                    {
                                        text.Span("ชื่อเล่น: ").FontSize(12).Bold();
                                        text.Span($"{_form["Nickname"] ?? ""}").FontSize(12);
                                    }
                                );
                            col.RelativeItem(4);
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem().Padding(3).Text(
                                    text =>
                                    {
                                        text.Span("Name-Surname[EN]: ").FontSize(12).Bold();
                                        text.Span($"{_form["FirstNameEng"] ?? ""} {_form["LastNameEng"] ?? ""}").FontSize(12);
                                    }
                                );
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            var birthDateText = FormatBuddhistDate(_form["BirthDate"], "DD MMM YYYY");
                            col.RelativeItem(4).Padding(3).Text(
                                text =>
                                {
                                    text.Span("วัน/เดือน/ปี เกิด [Date Of Birth]: ").FontSize(12).Bold();
                                    text.Span(birthDateText).FontSize(12);
                                }
                            );
                            col.ConstantItem(75).AlignLeft().Padding(3).Text(
                                text =>
                                {
                                    text.Span("อายุ [Age]: ").FontSize(12).Bold();
                                    text.Span($"{_form["Age"]} ปี").FontSize(12);
                                }
                            );
                            col.RelativeItem(2).Padding(3).Text(
                                text =>
                                {
                                    text.Span("น้ำหนัก [Weight]: ").FontSize(12).Bold();
                                    text.Span($"{_form["Weight"] ?? ""} กก.").FontSize(12);
                                }
                            );
                            col.RelativeItem(2).AlignLeft().Padding(3).Text(
                                text =>
                                {
                                    text.Span("ส่วนสูง [Height]: ").FontSize(12).Bold();
                                    text.Span($"{_form["Height"] ?? ""} ซม.").FontSize(12);
                                }
                            );
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(4).Padding(3).Text(
                                text =>
                                {
                                    text.Span("บัตรประจำตัวประชาชน[CitizenID]: ").FontSize(12).Bold();
                                    text.Span($"{_form["CitizenID"] ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(4).Padding(3).Text(
                                text =>
                                {
                                    text.Span("ออกให้ ณ [Issued by]: ").FontSize(12).Bold();
                                    text.Span($"{_form["CitizenIDIssuedBy"]}").FontSize(12);
                                }
                            );
                            var CitizenIDExpiry = FormatBuddhistDate(_form["CitizenIDExpiresON"], "DD MMM YYYY");
                            col.RelativeItem(4).Padding(3).Text(
                                text =>
                                {
                                    text.Span("บัตรหมดอายุวันที่ [Expiry date]: ").FontSize(12).Bold();
                                    text.Span($"{CitizenIDExpiry}").FontSize(12);
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
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("เบอร์โทรศัพท์[Mobile Phone]: ").FontSize(12).Bold();
                                    text.Span($"{_form["MobilePhone"] ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("LINE ID: ").FontSize(12).Bold();
                                    text.Span($"{_form["LINE"] ?? "-"}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
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
                                RenderCheckBox(row, _form["MinitaryService"]?.ToString() ?? "", "completed", "ผ่านการเกณฑ์ทหาร", "Completed");
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["MinitaryService"]?.ToString() ?? "", "no completed", "ยังไม่ได้เกณฑ์ทหาร", "No Completed");
                            });
                            col.RelativeItem(6).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["MinitaryService"]?.ToString() ?? "", "exemted", $"ได้รับการยกเว้น เนื่องจาก: {_form["ReasonMinitary"] ?? "............................."}", "Exemted,Please specific");
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
                            col.RelativeItem(6).Padding(3).Text(
                                text =>
                                {
                                    text.Span("ชื่อคู่สมรส [Spouse's Name]: ").FontSize(12).Bold();
                                    text.Span($"{_form["SpouseFullName"] ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["SpouseAliveStatus"]?.ToString() ?? "", "alive", "มีชีวิต", "");
                                RenderCheckBox(row, _form["SpouseAliveStatus"]?.ToString() ?? "", "deceased", "ถึงแก่กรรม", "");
                            });
                            col.RelativeItem(5).Padding(4).Text(
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
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("เบอร์โทรศัพท์[Mobile Phone]: ").FontSize(12).Bold();
                                    text.Span($"{_form["SpouseMobilePhone"] ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("LINE ID: ").FontSize(12).Bold();
                                    text.Span($"{_form["SpouseLINE"] ?? "-"}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("Email: ").FontSize(12).Bold();
                                    text.Span($"{_form["SpouseEmail"] ?? ""}").FontSize(12);
                                }
                            );
                        });
                    });
                    // ---------------------------------------------- ข้อมูลครอบครัว ----------------------------------------------
                    var ReferenceList = new List<RelationshipDto>();
                    RelationshipDto? Father = null;
                    RelationshipDto? Mother = null;
                    if (_form["RelationshipList"] != null && _form["RelationshipList"] != DBNull.Value)
                    {
                        var options = new JsonSerializerOptions
                        {
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };
                        var relationships = JsonSerializer.Deserialize<List<RelationshipDto>>(
                            _form["RelationshipList"]?.ToString() ?? "[]", options
                        ) ?? new List<RelationshipDto>();
                        Father = relationships.FirstOrDefault(r => r.RELATION_TYPE == "Father");
                        Mother = relationships.FirstOrDefault(r => r.RELATION_TYPE == "Mother");
                    }
                    col.Item().PaddingTop(5).Column(innerRow =>
                    {
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(4).Padding(3).Text(
                                text =>
                                {
                                    text.Span("ชื่อ-สกุล บิดา: ").FontSize(12).Bold();
                                    text.Span($"{Father?.NAMESURNAME ?? ""}").FontSize(12);
                                }
                            );
                            col.ConstantItem(50).Padding(3).Text(
                                text =>
                                {
                                    text.Span("อายุ: ").FontSize(12).Bold();
                                    text.Span($"{Father?.AGE} ปี").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("อาชีพ: ").FontSize(12).Bold();
                                    text.Span($"{Father?.CAREER ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).Padding(3).Text(
                                text =>
                                {
                                    text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                    text.Span($"{Father?.MOBILE ?? ""}").FontSize(12);
                                }
                            );
                            col.RelativeItem(3).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, Father?.ALIVE_STATUS ?? "", "alive", "มีชีวิต", "");
                                RenderCheckBox(row, Father?.ALIVE_STATUS ?? "", "deceased", "ถึงแก่กรรม", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(4).Padding(3).Text(text =>
                            {
                                text.Span("ชื่อ-สกุล มารดา: ").FontSize(12).Bold();
                                text.Span($"{Mother?.NAMESURNAME ?? ""}").FontSize(12);
                            });
                            col.ConstantItem(50).Padding(3).Text(text =>
                            {
                                text.Span("อายุ: ").FontSize(12).Bold();
                                text.Span($"{Mother?.AGE} ปี").FontSize(12);
                            });
                            col.RelativeItem(3).Padding(3).Text(text =>
                            {
                                text.Span("อาชีพ: ").FontSize(12).Bold();
                                text.Span($"{Mother?.CAREER ?? ""}").FontSize(12);
                            });
                            col.RelativeItem(3).Padding(3).Text(text =>
                            {
                                text.Span("เบอร์โทร: ").FontSize(12).Bold();
                                text.Span($"{Mother?.MOBILE ?? ""}").FontSize(12);
                            });
                            col.RelativeItem(3).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, Mother?.ALIVE_STATUS ?? "", "alive", "มีชีวิต", "");
                                RenderCheckBox(row, Mother?.ALIVE_STATUS ?? "", "deceased", "ถึงแก่กรรม", "");
                            });
                        });
                        var RelationshipList = new List<RelationshipDto>();
                        if (_form["RelationshipList"] != null && _form["RelationshipList"] != DBNull.Value)
                        {
                            var options = new JsonSerializerOptions
                            {
                                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            };
                            RelationshipList = JsonSerializer.Deserialize<List<RelationshipDto>>(
                            _form["RelationshipList"].ToString() ?? "[]", options
                                )?
                                .Where(r => r.RELATION_TYPE == "Sibling")
                                .ToList()
                                ?? new List<RelationshipDto>();
                        }
                        while (RelationshipList.Count < 4)
                        {
                            RelationshipList.Add(new RelationshipDto());
                        }
                        innerRow.Item().PaddingTop(1).PaddingBottom(1).Row(col =>
                        {
                            col.RelativeItem().PaddingTop(5).Text($"ท่านมีพี่-น้องจำนวน {_form["SiblingsAll"]?.ToString() ?? ""} คน ท่านเป็นคนที่ {_form["NumberAY"]} (กรุณากรอกรายละเอียดของพี่น้องที่ประกอบอาชีพ)").FontSize(12).Bold();
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
                                    columns.RelativeColumn(5);
                                    columns.RelativeColumn(2);
                                });
                                // Header
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("ชื่อ-สกุล").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("อายุ").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("อาชีพ/ตำแหน่ง").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("บริษัท").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("เบอร์โทรศัพท์").FontSize(12).Bold();
                                // Data Row
                                foreach (var Sibling in RelationshipList)
                                {
                                    table.Cell().Border(1).Padding(3)
                                        .Text(Sibling.NAMESURNAME ?? "").FontSize(12).AlignCenter();
                                    table.Cell().Border(1).Padding(3)
                                        .Text(Sibling.AGE ?? "").FontSize(12).AlignCenter();
                                    table.Cell().Border(1).Padding(3)
                                        .Text(Sibling.CAREER ?? "").FontSize(12).AlignCenter();
                                    table.Cell().Border(1).Padding(3)
                                        .Text(Sibling.COMPANY ?? "")
                                        .FontSize(12).AlignCenter()
                                        .WrapAnywhere();
                                    table.Cell().Border(1).Padding(3)
                                        .Text(Sibling.MOBILE ?? "")
                                        .FontSize(12).AlignCenter()
                                        .WrapAnywhere();
                                }
                            });
                        });
                        var UrgentList = new List<RelationshipDto>();
                        RelationshipDto? Urgent = null;
                        if (_form["RelationshipList"] != null && _form["RelationshipList"] != DBNull.Value)
                        {
                            var options = new JsonSerializerOptions
                            {
                                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            };
                            var relationships = JsonSerializer.Deserialize<List<RelationshipDto>>(
                                _form["RelationshipList"]?.ToString() ?? "[]", options
                            ) ?? new List<RelationshipDto>();
                            Urgent = relationships.FirstOrDefault(r => r.RELATION_TYPE == "Urgent");
                        }
                        // ถ้า Urgent เป็น null → สร้าง object เปล่าให้ 1 ตัว
                        if (Urgent == null)
                        {
                            UrgentList.Add(new RelationshipDto());
                        }
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
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(7);
                                });
                                // Header
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("ชื่อ-สกุล").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("ความสัมพันธ์").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("เบอร์โทรศัพท์").FontSize(12).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Black)
                                    .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                    .Text("ที่อยู่").FontSize(12).Bold();
                                // Data Row
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(Urgent?.NAMESURNAME ?? "").FontSize(12);
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(Urgent?.RELATION_DESCRIPTION ?? "").FontSize(12);
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(Urgent?.MOBILE ?? "").FontSize(12);
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(Urgent?.ADDRESS ?? "")
                                    .FontSize(12)
                                    .WrapAnywhere();
                            });
                        });
                    });
                }); // Close Content Page 1
            }); // Close container Page 1
            // -------------------------------------------------------------- Page 2 --------------------------------------------------------------
            container.Page(page =>
            {
                page.Content().Column(col =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(8);
                    page.MarginHorizontal(8);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DB Heavent"));
                    var educationList = new List<EducationsV3Dto>();
                    if (_form["EducationList"] != null && _form["EducationList"] != DBNull.Value)
                    {
                        var options = new JsonSerializerOptions
                        {
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };
                        educationList = JsonSerializer.Deserialize<List<EducationsV3Dto>>(
                            _form["EducationList"].ToString() ?? "[]", options
                        ) ?? new List<EducationsV3Dto>();
                    }
                    while (educationList.Count < 4)
                    {
                        educationList.Add(new EducationsV3Dto());
                    }
                    col.Item().Padding(5).Text("ข้อมูลประวัติการศึกษา (Educational Details)").FontSize(12).Bold();
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ระดับการศึกษา").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("วุฒิการศึกษา / สาขา").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ชื่อสถานศึกษา").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("จังหวัด/ประเทศ").FontSize(12).Bold();
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ปีการศึกษา").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("GPA").FontSize(12).Bold();
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี").FontSize(12).Bold();
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี").FontSize(12).Bold();
                        foreach (var edu in educationList)
                        {
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.EducationLevel ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.Major ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.InstitutionName ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.Province ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.StartYear?.ToString() ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.EndYear?.ToString() ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(edu.GPA == null || edu.GPA == 0 ? "" : edu.GPA.ToString()).FontSize(12);
                        }
                    });
                    var workList = new List<WorkExperiencesV3Dto>();
                    if (_form["WorkExperienceList"] != null && _form["WorkExperienceList"] != DBNull.Value)
                    {
                        workList = JsonSerializer.Deserialize<List<WorkExperiencesV3Dto>>(
                            _form["WorkExperienceList"].ToString() ?? "[]"
                        ) ?? new List<WorkExperiencesV3Dto>();
                    }
                    while (workList.Count < 10)
                    {
                        workList.Add(new WorkExperiencesV3Dto());
                    }
                    col.Item().Padding(5).Text("ข้อมูลประวัติการทำงาน (Work Experiences)").FontSize(12).Bold();
                    col.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1); // index
                            columns.RelativeColumn(2); // Start
                            columns.RelativeColumn(2); // End
                            columns.RelativeColumn(2); // ExperiencesYear
                            columns.RelativeColumn(3); // Position
                            columns.RelativeColumn(5); // Company
                            columns.RelativeColumn(2); // Salary
                        });
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ลำดับที่").FontSize(12).Bold();
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ระยะเวลา").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("อายุงาน").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("ตำแหน่ง").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("บริษัท").FontSize(12).Bold();
                        table.Cell().RowSpan(2).Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("เงินเดือนสุดท้าย").FontSize(12).Bold();
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ตั้งแต่ปี").FontSize(12).Bold();
                        table.Cell().Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle()
                            .Text("ถึงปี").FontSize(12).Bold();
                        foreach (var (work, index) in workList.Select((w, i) => (w, i)))
                        {
                            var workStart = FormatBuddhistDate(work.StartDate, "MMM YYYY");
                            var workEnd = FormatBuddhistDate(work.EndDate, "MMM YYYY");
                            // ✅ แถวที่ 1: ข้อมูลหลัก
                            table.Cell().RowSpan(2).Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text((index + 1).ToString()).FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(workStart ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(workEnd ?? "").FontSize(12);
                            // สมมติอยากแสดง "อายุงาน" ตรงกลาง (ตามภาพ = 3 ปี)
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(work.WorkDuration).FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(work.Position ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(work.CompanyName ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter().AlignMiddle()
                                .Text(work.Salary ?? "").FontSize(12);
                            // ✅ แถวที่ 2: ลักษณะงานโดยสังเขป (สร้างเสมอ)
                            table.Cell().ColumnSpan(6).Border(1).Padding(3).MinHeight(30)
                            .Text(text =>
                            {
                                text.Span("ลักษณะงานโดยสังเขป: ").FontSize(12).Bold();
                                if (!string.IsNullOrWhiteSpace(work.Responsibilities))
                                    text.Span(work.Responsibilities).FontSize(12);
                                else
                                    text.Span(" ").FontSize(12); // เว้น space ให้มีเนื้อหา
                            });
                        }
                    });
                });
            });
            // -------------------------------------------------------------- Page 3 --------------------------------------------------------------
            container.Page(page =>
            {
                page.Content().Column(col =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(8);
                    page.MarginHorizontal(8);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DB Heavent"));
                    col.Item().Padding(5).Text("ความสามารถพิเศษ").FontSize(12).Bold();
                    col.Item().Column(innerRow =>
                    {
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            var skills = new List<SkillsV3Dto>();
                            SkillsV3Dto? Other = null;
                            SkillsV3Dto? Hobbies = null;
                            SkillsV3Dto? Activites = null;
                            SkillsV3Dto? Interests = null;
                            if (_form["SkillsList"] != null && _form["SkillsList"] != DBNull.Value)
                            {
                                var options = new JsonSerializerOptions
                                {
                                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                                };
                                skills = JsonSerializer.Deserialize<List<SkillsV3Dto>>(
                                    _form["SkillsList"]?.ToString() ?? "[]", options
                                ) ?? new List<SkillsV3Dto>();
                                Other = JsonSerializer.Deserialize<List<SkillsV3Dto>>(
                                    _form["SkillsList"].ToString() ?? "[]", options
                                )?
                                .FirstOrDefault(r => r.SkillType == "Other");
                                Hobbies = JsonSerializer.Deserialize<List<SkillsV3Dto>>(
                                    _form["SkillsList"].ToString() ?? "[]", options
                                )?
                                .FirstOrDefault(r => r.SkillType == "Hobbies");
                                Activites = JsonSerializer.Deserialize<List<SkillsV3Dto>>(
                                    _form["SkillsList"].ToString() ?? "[]", options
                                )?
                                .FirstOrDefault(r => r.SkillType == "Activites");
                                Interests = JsonSerializer.Deserialize<List<SkillsV3Dto>>(
                                    _form["SkillsList"].ToString() ?? "[]", options
                                )?
                                .FirstOrDefault(r => r.SkillType == "Interests");
                            }
                            if (skills.Count == 0)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    skills.Add(new SkillsV3Dto
                                    {
                                        SkillType = "",
                                        SkillDescription = "",
                                        SkillScore = null
                                    });
                                }
                            }
                            col.RelativeItem().Border(1).BorderColor(Colors.Black).Column(col =>
                            {
                                col.Item().PaddingLeft(5).Text("ภาษาต่างประเทศ [Language]").FontSize(12).Bold();
                                // รายการ TOEIC / TOEFL / IELTS
                                col.Item().PaddingLeft(8).Row(row =>
                                {
                                    foreach (var skill in skills)
                                    {
                                        if (skill.SkillType == "TOEIC" || skill.SkillType == "TOEFL" || skill.SkillType == "IELTS")
                                        {
                                            row.ConstantItem(80).Text(text =>
                                            {
                                                text.Span($"{skill.SkillType ?? ""}: ").FontSize(12).Bold();
                                                text.Span($"{skill.SkillScore ?? 0} คะแนน").FontSize(12);
                                            });
                                        }
                                    }
                                });
                                col.Item().PaddingLeft(5).Text("ภาษาอื่นๆ [Orther Language]").FontSize(12).Bold();
                                col.Item().PaddingLeft(8).PaddingRight(5).Row(row =>
                                {
                                    row.RelativeItem().Text($"{Other?.SkillType}: {Other?.SkillScore} คะแนน").FontSize(12);
                                });
                                col.Item().PaddingLeft(5).Text("งานอดิเรก [Hobbies]").FontSize(12).Bold();
                                col.Item().PaddingLeft(8).PaddingRight(5).Row(row =>
                                {
                                    row.RelativeItem().Text(Hobbies?.SkillDescription).FontSize(12);
                                });
                                col.Item().PaddingLeft(5).Text("กิจกรรม [Activites]").FontSize(12).Bold();
                                col.Item().PaddingLeft(8).PaddingRight(5).Row(row =>
                                {
                                    row.RelativeItem().Text(Activites?.SkillDescription).FontSize(12);
                                });
                                col.Item().PaddingLeft(5).Text("กีฬาที่ท่านสนใจ [Interests]").FontSize(12).Bold();
                                col.Item().PaddingLeft(8).PaddingRight(5).Row(row =>
                                {
                                    row.RelativeItem().Text(Interests?.SkillDescription).FontSize(12);
                                });
                            });
                            col.RelativeItem().Column(col =>
                            {
                                foreach (var skill in skills)
                                {
                                    if (skill.SkillType == "ComSkill" || skill.SkillType == "genius")
                                    {
                                        col.Item().Border(1).BorderColor(Colors.Black).Column(box =>
                                        {
                                            // Header
                                            box.Item().PaddingLeft(5)
                                                .Text(skill.SkillType == "ComSkill"
                                                    ? "ความรู้ทางคอมพิวเตอร์ (Computer Skills)"
                                                    : "ความรู้หรือทักษะอื่น ๆ (Other Skills)")
                                                .FontSize(12).Bold();
                                            // ถ้ามีข้อความ → แตกเป็นหลายบรรทัด
                                            var desc = (skill.SkillDescription ?? "").Split('\n');
                                            foreach (var line in desc)
                                            {
                                                box.Item()
                                                    .PaddingHorizontal(5)
                                                    .MinHeight(20)
                                                    .PaddingLeft(8)
                                                    .Element(e =>
                                                    {
                                                        e.ScaleToFit()
                                                        .Text(line)
                                                        .FontSize(12)
                                                        .WrapAnywhere();
                                                    });
                                            }
                                            // ถ้าไม่มี หรือบรรทัดน้อยกว่า 3 → เติม blank row
                                            int filled = desc.Length;
                                            for (int i = filled; i < 3; i++)
                                            {
                                                box.Item().PaddingHorizontal(5).Height(20);
                                            }
                                        });
                                    }
                                }
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Text("ท่านมียานพาหนะเป็นของตนเองหรือไม่ รถจักรยานยนต์[Motorcycle]").FontSize(12).Bold();
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireVehiclesMotorcycle"]?.ToString() ?? "", "no", "ไม่มี[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireVehiclesMotorcycle"]?.ToString() ?? "", "yes", $"มี[Yes] License:{_form["MotorcycleLicense"]?.ToString() ?? ""}", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).PaddingTop(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Text("ท่านมียานพาหนะเป็นของตนเองหรือไม่ รถยนต์[Car]").FontSize(12).Bold();
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireVehiclesCar"]?.ToString() ?? "", "no", "ไม่มี[No]", "");
                            });
                            col.RelativeItem(4).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireVehiclesCar"]?.ToString() ?? "", "yes", $"มี[Yes] License: {_form["CarLicense"]?.ToString() ?? ""}", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านมีความบกพร่องทางร่างกาย หรือ หย่อนสมรรถภาพอื่นๆ หรือไม่").FontSize(12).Bold();
                                col.Item().Text("Do you have any physical handicapped, chronic or other disabilities ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireDisabilities"]?.ToString() ?? "", "no", "ไม่มี[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireDisabilities"]?.ToString() ?? "", "yes", "มี/โปรดระบุ[Yes]", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านเคยถูกจับหรือเคยต้องคดีอาญาหรือไม่").FontSize(12).Bold();
                                col.Item().Text("Have you ever been arrested or convicted of a criminal case ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireConvicted"]?.ToString() ?? "", "no", "ไม่เคย[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireConvicted"]?.ToString() ?? "", "yes", "เคย[Yes]", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านเคยถูกไล่ออกจากงาน เนื่องจากความประพฤติ หรืองานไม่ดีพอ หรือไม่").FontSize(12).Bold();
                                col.Item().Text("Have you ever been discharged from employment because of your conduct or unperformed ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireFiredjob"]?.ToString() ?? "", "no", "ไม่เคย[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireFiredjob"]?.ToString() ?? "", "yes", "เคย[Yes]", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านสามารถจัดหาผู้ค้ำประกันการเข้าทำงานของท่านได้หรือไม่").FontSize(12).Bold();
                                col.Item().Text("Do you mind to provide guarantor for applying this job ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireApplyjob"]?.ToString() ?? "", "no", "ไม่มี[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireApplyjob"]?.ToString() ?? "", "yes", "มี[Yes]", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านสามารถทำงานเป็นกะได้หรือไม่").FontSize(12).Bold();
                                col.Item().Text("Do you mind to work on shift basis ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireWorkShifts"]?.ToString() ?? "", "no", "ไม่ได้[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireWorkShifts"]?.ToString() ?? "", "yes", "ได้[Yes]", "");
                            });
                        });
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านยินดีให้บริษัทสอบถาม ตรวจสอบคุณวุฒิกับบริษัทที่ทำงานอยู่ปัจจุบัน หรือไม่").FontSize(12).Bold();
                                col.Item().Text("If currently employed, can we contact your current employer to check information ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireCheckInformation"]?.ToString() ?? "", "no", "ไม่ยินยอม[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireCheckInformation"]?.ToString() ?? "", "yes", "ยินยอม[Yes]", "");
                            });
                        });
                        RelationshipDto? Relative = null;
                        if (_form["RelationshipList"] != null && _form["RelationshipList"] != DBNull.Value)
                        {
                            var options = new JsonSerializerOptions
                            {
                                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            };
                            var relationships = JsonSerializer.Deserialize<List<RelationshipDto>>(
                                _form["RelationshipList"]?.ToString() ?? "[]", options
                            ) ?? new List<RelationshipDto>();
                            Relative = relationships.FirstOrDefault(r => r.RELATION_TYPE == "Relative");
                        }
                        innerRow.Item().PaddingLeft(5).Row(col =>
                        {
                            col.RelativeItem(6).Padding(3).Column(col =>
                            {
                                col.Item().Text("ท่านมีญาติที่ทำงานอยู่ในบริษัทนี้หรือไม่").FontSize(12).Bold();
                                col.Item().Text("Do you have relatives working in this company ?").FontSize(12).Bold();
                            });
                            col.RelativeItem(2).PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireRelative"]?.ToString() ?? "", "no", "ไม่มี[No]", "");
                            });
                            col.RelativeItem(4).AlignLeft().PaddingTop(4).Row(row =>
                            {
                                RenderCheckBox(row, _form["QuestionnaireRelative"]?.ToString() ?? "", "yes", $"มี[Yes] ชื่อ-สกุล: {Relative?.NAMESURNAME ?? "..................................."} \n ตำแหน่ง: {Relative?.CAREER ?? "............................. "}", "");
                            });
                        });
                    });
                    var ReferenceList = new List<RelationshipDto>();
                    RelationshipDto? Reference = null;
                    if (_form["RelationshipList"] != null && _form["RelationshipList"] != DBNull.Value)
                    {
                        var options = new JsonSerializerOptions
                        {
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };
                        var relationships = JsonSerializer.Deserialize<List<RelationshipDto>>(
                            _form["RelationshipList"]?.ToString() ?? "[]", options
                        ) ?? new List<RelationshipDto>();
                        Reference = relationships.FirstOrDefault(r => r.RELATION_TYPE == "Reference");
                    }
                    // ถ้า Reference เป็น null → สร้าง object เปล่าให้ 1 ตัว
                    if (Reference == null)
                    {
                        ReferenceList.Add(new RelationshipDto());
                    }
                    col.Item().Padding(5).Text("บุคคลอ้างอิง [References]").FontSize(12).Bold();
                    col.Item().Column(innerRow =>
                    {
                        innerRow.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(7);
                                columns.RelativeColumn(2);
                            });
                            // Header
                            table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                .Text("ชื่อ-สกุล").FontSize(12).Bold();
                            table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                .Text("ความสัมพันธ์").FontSize(12).Bold();
                            table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                .Text("ตำแหน่ง").FontSize(12).Bold();
                            table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                .Text("บริษัท").FontSize(12).Bold();
                            table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                                .Text("เบอร์โทรศัพท์").FontSize(12).Bold();
                            // Data Row
                            table.Cell().Border(1).Padding(3).AlignCenter()
                                .Text(Reference?.NAMESURNAME ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter()
                                .Text(Reference?.RELATION_DESCRIPTION ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter()
                                .Text(Reference?.CAREER ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter()
                                .Text(Reference?.COMPANY ?? "").FontSize(12);
                            table.Cell().Border(1).Padding(3).AlignCenter()
                                .Text(Reference?.MOBILE ?? "")
                                .FontSize(12)
                                .WrapAnywhere();
                        });
                        innerRow.Item().PaddingLeft(5).PaddingTop(5).Row(col =>
                        {
                            col.RelativeItem().Padding(3).Column(col =>
                            {
                                col.Item().PaddingLeft(10).Text("ข้าพเจ้าขอรับรองว่า ข้อความทั้งหมดในใบสมัครงานและเอกสารอื่นๆ ที่นำส่งแก่บริษัทเป็นความจริง ถูกต้อง และเอกสารครบถ้วน สมบูรณ์ทุกประการ หากปรากฎว่าข้อความใน").FontSize(12);
                                col.Item().Text("ใบสมัครและเอกสารที่นำมาแสดง หรือรายละเอียดที่ให้ไม่เป็นความจริง ไม่ถูกต้อง หรือไม่ครบถ้วน ข้าพเจ้ายินยอมให้บริษัทใช้สิทธิ เลิกจ้างโดยทันที โดยไม่ต้อง มีการบอกกล่าวล่วงหน้า และโดยไม่ต้องจ่ายค่าชดเชย หรือ ค่าเสียหายใด ๆ ทั้งสิ้น").FontSize(12);
                                col.Item().PaddingLeft(5).Text("I hereby certify that all statements given in this application and all documents submitted to the company are true, complete and accurate in all respects.").FontSize(12);
                                col.Item().Text("If any part of my statement or the documents submitted to the company is found to be untrue, incomplete or inaccurate, I hereby agree that company has the ").FontSize(12);
                                col.Item().Text("right to immediately terminate my employment without having to pay any compensation, severance pay, or damage whatsoever without advance notice.").FontSize(12);
                            });
                        });
                    });
                    page.Footer().AlignRight().Column(col =>
                    {
                        var DateNow = FormatBuddhistDate(DateTime.Now, "DD MMM YYYY");
                        col.Item().Padding(5).Text("ลงชื่อผู้สมัคร ............................................").FontSize(12);
                        col.Item().Padding(5).Text("Signature (..........................................)").FontSize(12);
                        col.Item().AlignCenter().Padding(5).Text($"Date: {DateNow}").FontSize(12);
                    });
                }); // Close container Page 2
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
        private static string FormatBuddhistDate(object? value, string? format)
        {
            if (DateTime.TryParse(value?.ToString(), out var dt))
            {
                if (format == "DD MMM YYYY")
                {
                    return $"{dt.Day} {dt.ToString("MMM", new System.Globalization.CultureInfo("en-US"))} {dt.Year + 543}";
                }
                if (format == "DD-MMM-YYYY")
                {
                    return $"{dt.Day}-{dt.ToString("MMM", new System.Globalization.CultureInfo("en-US"))}-{dt.Year + 543}";
                }
                if (format == "MMM YYYY")
                {
                    return $"{dt.ToString("MMM", new System.Globalization.CultureInfo("en-US"))} {dt.Year + 543}";
                }
                if (format == "MMM-YYYY")
                {
                    return $"{dt.ToString("MMM", new System.Globalization.CultureInfo("en-US"))} - {dt.Year + 543}";
                }
            }
            return "";
        }
        private void ComposeFirstPageHeader(IContainer container)
        {
            container.Row(row =>
            {
                // ✅ ซ้าย: โลโก้ + ข้อความบริษัท (กลางหน้า)
                row.RelativeItem(17).Column(col =>
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");
                    // โลโก้ตรงกลาง
                    col.Item().AlignCenter().PaddingLeft(50).Width(80).Image(imagePath).FitWidth();
                    col.Item().AlignCenter().PaddingLeft(50).Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(12).Bold();
                    col.Item().AlignCenter().PaddingLeft(50).Text("The ONE Enterprise Public Company Limited").FontSize(12);
                    // กรอบเต็มความกว้าง (ยาวเหมือนภาพ 2)
                    col.Item().PaddingTop(10);
                    col.Item().Border(1).BorderColor(Colors.Black).Padding(8).Row(innerRow =>
                    {
                        // ชื่อบริษัท
                        innerRow.RelativeItem(6).PaddingLeft(5).Column(left =>
                        {
                            left.Item().Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)").FontSize(12).Bold();
                            left.Item().Text("The ONE Enterprise Public Company Limited").FontSize(12).Bold();
                        });
                        // วันที่พร้อมเริ่มงาน + อัตราเงินเดือนที่ต้องการ
                        innerRow.RelativeItem(6).AlignCenter().Column(right =>
                        {
                            var jobStartDateText = FormatBuddhistDate(_form["JobStartDate"], "DD MMM YYYY");
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
                    col.Item().Border(1).BorderColor(Colors.Black).PaddingTop(9).PaddingBottom(6).PaddingLeft(5).Row(innerRow =>
                    {
                        // ตำแหน่งที่ต้องการสมัคร
                        innerRow.RelativeItem(6).Column(left =>
                        {
                            left.Item().PaddingLeft(5).Text(
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
    public class EducationsV3Dto
    {
        public string? EducationLevel { get; set; }
        public string? InstitutionName { get; set; }
        public string? Province { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? Major { get; set; }
        public decimal? GPA { get; set; }
    }
    public class WorkExperiencesV3Dto
    {
        public string? CompanyName { get; set; }
        public string? Position { get; set; }
        public string? Responsibilities { get; set; }
        public string? ReasonForLeaving { get; set; }
        public string? Salary { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? WorkDuration { get; set; }
    }
    public class SkillsV3Dto
    {
        public string? SkillType { get; set; }
        public string? SkillDescription { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? SkillScore { get; set; }
    }
    public class RelationshipDto
    {
        public string? RELATION_TYPE { get; set; }
        public string? NAMESURNAME { get; set; }
        public string? RELATION_DESCRIPTION { get; set; }
        public string? CAREER { get; set; }
        public string? COMPANY { get; set; }
        public string? AGE { get; set; }
        public string? MOBILE { get; set; }
        public string? ALIVE_STATUS { get; set; }
        public string? ADDRESS { get; set; }
        public string? RelativeFullName { get; set; }
        public string? RelativeOccupation { get; set; }
    }
}