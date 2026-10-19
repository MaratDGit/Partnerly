using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserViewModel : IAuditableViewModel
    {
        #region ID
        [Required]
        public Guid Id { get; set; }
        #endregion

        #region FirstName
        [CustomStringLenght(min: 4, max: 20)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.FirstName), ResourceType = typeof(FieldsDisplayNames))]
        public string? FirstName { get; set; }
        #endregion

        #region LastName
        [CustomStringLenght(min: 4, max: 20)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.LastName), ResourceType = typeof(FieldsDisplayNames))]
        public string? LastName { get; set; }
        #endregion

        #region Email
        [EmailAddress]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [ArmenianPhone(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.TypeValidPhoneNumber))]
        [Display(Name = nameof(FieldsDisplayNames.Phone), ResourceType = typeof(FieldsDisplayNames))]
        public string? Phone { get; set; }
        #endregion

        #region PhotoUrl
        [Display(Name = nameof(FieldsDisplayNames.Photo), ResourceType = typeof(FieldsDisplayNames))]
        public string? PhotoUrl { get; set; }
        #endregion

        #region Balance
        [Display(Name = nameof(FieldsDisplayNames.Balance), ResourceType = typeof(FieldsDisplayNames))]
        public decimal? Balance { get; set; }
        #endregion

        #region RoleId
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public Guid? RoleId { get; set; }
        #endregion

        #region RoleName
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public string? RoleName { get; set; }
        #endregion

        #region ReferralCode
        [Required]
        [Display(Name = nameof(FieldsDisplayNames.ReferrerCode), ResourceType = typeof(FieldsDisplayNames))]
        public string? MyReferralCode { get; set; }
        #endregion

        #region ReferrerId
        [Display(Name = "Referrer")]
        public Guid? ReferrerId { get; set; }
        #endregion

        #region LastActivity
        [Display(Name = "Last Activity")]
        public DateTime? LastActivity { get; set; }
        #endregion

        #region IsOnlayn
        [Display(Name = "Is Onlayn")]
        public bool? IsOnlayn { get; set; } 
        #endregion

        #region IsBlocked
        [Display(Name = nameof(FieldsDisplayNames.IsBlocked), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsBlocked { get; set; }
        public bool IsBlockedBool { get => IsBlocked ?? false; set => IsBlocked = value; }
        #endregion

        #region EmailConfirmed
        [Display(Name = nameof(FieldsDisplayNames.EmailConfirmed), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EmailConfirmed { get; set; }
        public bool EmailConfirmedBool { get => EmailConfirmed ?? false; set => EmailConfirmed = value; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;

        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdaterName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public DataTableOptions users { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.usersTable =>
                   new DataTableOptions
                   {
                       Id = tableID,
                       Name = FieldsDisplayNames.Users,
                       Icon = "bx bx-group",

                       KeyField = "Id",
                       AjaxUrl = "/Users/Data",
                       ExportUrl = "/Users/Export",

                       AutoLoad = true,
                       ReloadEnabled = true,
                       RememberState = true,

                       InlineEditing = true,
                       InlineUpdateUrl = "/Users/UpdateInline",

                       Columns = new List<DataTableColumn>
                        {
                            new DataTableColumn
                            {
                                Field = "Id",
                                Title = "ID",
                                Type = DataTableColumnType.Number,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.Selected),
                                Type = DataTableColumnType.Boolean,
                                Searchable = false,
                                Exportable = false,
                                Alignment = DataTableAlignment.Center,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.FirstName),
                                Title = FieldsDisplayNames.FirstName,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/Users/Edit/{Id}",
                                Editable = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.LastName),
                                Title = FieldsDisplayNames.LastName,
                                Type = DataTableColumnType.Text,
                                Editable = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.Email),
                                Title = FieldsDisplayNames.Email,
                                Type = DataTableColumnType.Email,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.MyReferralCode),
                                Title = FieldsDisplayNames.ReferrerCode,
                                Type = DataTableColumnType.Text,
                                Alignment = DataTableAlignment.Center,
                                ConditionalStyles = new List<DataTableConditionalStyle>
                                {
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.NotEquals,
                                                Value = ""
                                            },
                                        },

                                        CssClass = "badge bg-label-secondary fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                },
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.IsBlocked),
                                Title = FieldsDisplayNames.IsBlocked,
                                Type = DataTableColumnType.Boolean,
                                Editable = true,
                                Alignment = DataTableAlignment.Center,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.IsOnlayn),
                                Title = FieldsDisplayNames.IsOnlayn,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.EmailConfirmed),
                                Title = FieldsDisplayNames.EmailConfirmed,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                            },
                            //new DataTableColumn
                            //{
                            //    Field = nameof(Models.User.Balance),
                            //    Title = FieldsDisplayNames.Balance,
                            //    Type = DataTableColumnType.Currency,
                            //    Editable = true,

                            //    ConditionalStyles = new List<DataTableConditionalStyle>
                            //    {
                            //        new DataTableConditionalStyle
                            //        {
                            //            Conditions = new List<DataTableConditionalCondition>
                            //            {
                            //                new DataTableConditionalCondition
                            //                {
                            //                    Operator = DataTableConditionalOperator.GreaterThanOrEqual,
                            //                    Value = "500"
                            //                },
                            //                new DataTableConditionalCondition
                            //                {
                            //                    Operator = DataTableConditionalOperator.LessThan,
                            //                    Value = "5000"
                            //                }
                            //            },
                            //            CssClass = "badge bg-label-warning fs-7 rounded-pill ms-auto text-uppercase",
                            //        },
                            //        new DataTableConditionalStyle
                            //        {
                            //            Conditions = new List<DataTableConditionalCondition>
                            //            {
                            //                new DataTableConditionalCondition
                            //                {
                            //                    Operator = DataTableConditionalOperator.LessThanOrEqual,
                            //                    Value = "500"
                            //                },
                            //            },
                            //            CssClass = "badge bg-label-danger fs-7 rounded-pill ms-auto text-uppercase",
                            //        },
                            //        new DataTableConditionalStyle
                            //        {
                            //            Conditions = new List<DataTableConditionalCondition>
                            //            {
                            //                new DataTableConditionalCondition
                            //                {
                            //                    Operator = DataTableConditionalOperator.GreaterThanOrEqual,
                            //                    Value = "5000"
                            //                },
                            //            },

                            //            CssClass = "badge bg-label-success fs-7 rounded-pill ms-auto text-uppercase",
                            //        },
                            //    },

                            //},
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.LastActivity),
                                Title = FieldsDisplayNames.LastActivity,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                                Resizable = true
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.User.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                                Resizable = true
                            }
                        },

                       Sorting = new DataTableSortOptions
                       {
                           DefaultField = "CreatedDate",

                       },

                       Filters = new DataTableFilterOptions
                       {
                           Enabled = true,
                           Items = new List<DataTableFilter>
                            {
                               new DataTableFilter
                                {
                                    Field = "RoleId",
                                    Label = FieldsDisplayNames.Role,
                                    Type = DataTableFilterType.MultiSelect,
                                    Placeholder = "Role Name",
                                    DefaultValue = "",
                                    Options = new List<DataTableFilterOption>
                                    {
                                        new DataTableFilterOption("5DAF66B1-8140-4D48-BC12-431086FA7144", FieldsDisplayNames.Admin),
                                        new DataTableFilterOption("C3120D74-9C4C-44AA-8B0C-A1B2F53BF9A8", FieldsDisplayNames.Employee),
                                        new DataTableFilterOption("52B55F50-A21A-441E-BB59-EFAB24F907AF", FieldsDisplayNames.User)
                                    }
                                },

                                new DataTableFilter
                                {
                                    Field = "IsBlocked",
                                    Label = FieldsDisplayNames.IsBlocked,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = "IsOnlayn",
                                    Label = FieldsDisplayNames.IsOnlayn,
                                    Type = DataTableFilterType.Boolean
                                },
                                //new DataTableFilter
                                //{
                                //    Field = "Balance",
                                //    Label = FieldsDisplayNames.Balance,
                                //    Type = DataTableFilterType.NumberRange
                                //},
                                
                                new DataTableFilter
                                {
                                    Field = "CreatedDate",
                                    Label = FieldsDisplayNames.CreatedDate,
                                    Type = DataTableFilterType.DateRange
                                }
                            }
                       },

                       Paging = new DataTablePagingOptions 
                       {
                           Enabled = true,
                           ShowPageSize = true
                       },

                       Actions = new DataTableActionOptions
                       {
                           Enabled = true,
                           Items = new List<DataTableAction>
                            {
                                new DataTableAction
                                {
                                    Name = "add",
                                    Title = FieldsDisplayNames.Create,
                                    Icon = "bx bx-plus",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Toolbar,
                                    Url = "/Users/Create",
                                    Method = "Get",
                                    CssClass = "data-table-grid-action"
                                },

                                new DataTableAction
                                {
                                    Name = "view",
                                    Title = FieldsDisplayNames.View,
                                    Icon = "bx bx-show",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Users/Edit/{Id}",
                                    Method = "GET",
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,
                                },

                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Users/Delete/{Id}",
                                    Method = "GET",
                                    CssClass = "btn btn-sm btn-outline-danger"
                                }
                            }
                       },
                   },

                _ => throw new ArgumentException(
                    $"Unknown DataTable: {tableID}")
            };

            _dataTableRegistry.Register(options);

            return options;
        }
    }
}
