import io

p = 'src/Roivo.Web/Components/Pages/TaxCalendar.razor'
s = io.open(p, encoding='utf-8').read()

old = '''                    <MudSimpleTable Dense="true" Hover="true">
                        <thead>
                            <tr>
                                <th>@Roivo.Resources.Cashflow.CategoryType</th>
                                <th>@Roivo.Resources.Cashflow.Period</th>
                                <th>@Roivo.Resources.Cashflow.EstimatedAmount</th>
                                <th>@Roivo.Resources.Cashflow.ActualAmount</th>
                                <th></th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var obligation in group.OrderBy(o => o.TaxType))
                            {
                                <tr>
                                    <td>@TaxTypeLabel(obligation.TaxType)</td>
                                    <td>@obligation.Period</td>
                                    <td>@FormatCurrency(obligation.EstimatedAmount)</td>
                                    <td>@(obligation.ActualAmount is { } actual ? FormatCurrency(actual) : "—")</td>
                                    <td>
                                        <MudChip T="string" Size="Size.Small" Color="@StatusColor(obligation)">
                                            @StatusLabel(obligation)
                                        </MudChip>
                                    </td>
                                    <td>
                                        @if (!obligation.IsPaid)
                                        {
                                            <MudButton Size="Size.Small"
                                                       Variant="Variant.Outlined"
                                                       StartIcon="@Icons.Material.Outlined.Done"
                                                       OnClick="@(() => OpenMarkPaid(obligation))">
                                                @Roivo.Resources.Cashflow.MarkAsPaid
                                            </MudButton>
                                        }
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </MudSimpleTable>'''

assert old in s, 'table block not found'

new = '''                    @* One table per due-date group meant the browser sized each
                       one independently and the columns drifted between groups.
                       Fixed widths make the groups scan as a single table. *@
                    <MudHidden Breakpoint="Breakpoint.Xs">
                        <div class="rv-tax-table">
                            <MudSimpleTable Dense="true" Hover="true">
                                <colgroup>
                                    <col style="width:30%" />
                                    <col style="width:12%" />
                                    <col style="width:15%" />
                                    <col style="width:15%" />
                                    <col style="width:13%" />
                                    <col style="width:15%" />
                                </colgroup>
                                <thead>
                                    <tr>
                                        <th>@Roivo.Resources.Cashflow.CategoryType</th>
                                        <th>@Roivo.Resources.Cashflow.Period</th>
                                        <th>@Roivo.Resources.Cashflow.EstimatedAmount</th>
                                        <th>@Roivo.Resources.Cashflow.ActualAmount</th>
                                        <th>@Reconciliation.Column_Status</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @foreach (var obligation in group.OrderBy(o => o.TaxType))
                                    {
                                        <tr>
                                            <td>@TaxTypeLabel(obligation.TaxType)</td>
                                            <td>@obligation.Period</td>
                                            <td>@FormatCurrency(obligation.EstimatedAmount)</td>
                                            <td>@(obligation.ActualAmount is { } actual ? FormatCurrency(actual) : "—")</td>
                                            <td class="rv-tax-status">
                                                <MudChip T="string" Size="Size.Small" Color="@StatusColor(obligation)">
                                                    @StatusLabel(obligation)
                                                </MudChip>
                                            </td>
                                            <td>
                                                @if (!obligation.IsPaid)
                                                {
                                                    <MudButton Size="Size.Small"
                                                               FullWidth="true"
                                                               Variant="Variant.Outlined"
                                                               StartIcon="@Icons.Material.Outlined.Done"
                                                               OnClick="@(() => OpenMarkPaid(obligation))">
                                                        @Roivo.Resources.Cashflow.MarkAsPaid
                                                    </MudButton>
                                                }
                                            </td>
                                        </tr>
                                    }
                                </tbody>
                            </MudSimpleTable>
                        </div>
                    </MudHidden>

                    @* Below sm the six columns cannot fit a phone, so each
                       obligation becomes a card led by its status. *@
                    <MudHidden Breakpoint="Breakpoint.Xs" Invert="true">
                        @foreach (var obligation in group.OrderBy(o => o.TaxType))
                        {
                            <MudPaper Outlined="true" Class="pa-3 mb-2">
                                <MudChip T="string" Size="Size.Small" Class="mb-2"
                                         Color="@StatusColor(obligation)">
                                    @StatusLabel(obligation)
                                </MudChip>
                                <MudText Typo="Typo.subtitle2">@TaxTypeLabel(obligation.TaxType)</MudText>
                                <MudText Typo="Typo.caption" Class="mud-text-secondary d-block mb-2">
                                    @Roivo.Resources.Cashflow.Period: @obligation.Period
                                </MudText>
                                <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-1">
                                    <MudText Typo="Typo.caption" Class="mud-text-secondary">@Roivo.Resources.Cashflow.EstimatedAmount</MudText>
                                    <MudText Typo="Typo.body2">@FormatCurrency(obligation.EstimatedAmount)</MudText>
                                </MudStack>
                                <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                                    <MudText Typo="Typo.caption" Class="mud-text-secondary">@Roivo.Resources.Cashflow.ActualAmount</MudText>
                                    <MudText Typo="Typo.body2">@(obligation.ActualAmount is { } cardActual ? FormatCurrency(cardActual) : "—")</MudText>
                                </MudStack>
                                @if (!obligation.IsPaid)
                                {
                                    <MudButton FullWidth="true"
                                               Variant="Variant.Outlined"
                                               StartIcon="@Icons.Material.Outlined.Done"
                                               OnClick="@(() => OpenMarkPaid(obligation))">
                                        @Roivo.Resources.Cashflow.MarkAsPaid
                                    </MudButton>
                                }
                            </MudPaper>
                        }
                    </MudHidden>'''

s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8', newline='').write(s)
print('ok')
