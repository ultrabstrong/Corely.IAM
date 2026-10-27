# ResourceTypePicker

Lists resource types grouped and searchable: Identity and access first, other groups by name,
Platform settings last, ungrouped types under Other, and types by title within a group. Each type
shows its title, its description and its code name, and long text wraps. A search box appears once
there are more than eight types.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Types` | `IEnumerable<ResourceTypeInfo>` | none | The types to offer |
| `IdPrefix` | `string` | none | Prefix of each input's id: `{IdPrefix}-{type name}` |
| `Multiple` | `bool` | `false` | Checkboxes rather than radios |
| `Cards` | `bool` | `false` | A grid of cards instead of a list, for a full width page |
| `ReadOnly` | `bool` | `false` | Only the chosen types, without inputs; "None" when there are none |
| `Value` / `ValueChanged` | `string?` | none | The chosen type, for a single choice |
| `Selected` / `SelectedChanged` | `IReadOnlySet<string>` | empty | The chosen types, for several |

## Usage

```razor
<ResourceTypePicker Types="_types" IdPrefix="newPermissionType"
                    Value="@_type" ValueChanged="OnTypeChangedAsync" />

<ResourceTypePicker Types="_types" IdPrefix="apiKeyType" Multiple="true" Cards="true"
                    ReadOnly="!_editing" Selected="_allowed" SelectedChanged="types => _allowed = new(types)" />
```

Create Permission uses the list; the API key types in the platform security settings use the cards.
