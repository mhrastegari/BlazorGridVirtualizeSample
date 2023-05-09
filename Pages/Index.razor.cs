using BlazorGridVirtualizeSample.Models;

using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace BlazorGridVirtualizeSample.Pages;

public partial class Index
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    public int WindowWidth { get; set; }

    private int _gridColumnCount = 3;
    private int _overScanCount = 5;
    private string _resizeEventListenerId = string.Empty;

    private DotNetObjectReference<Index>? _dotnetObjectReference;
    private Virtualize<Person[]>? _virtualizeGridRef;
    private List<Person> People = new();

    protected override async Task OnInitializedAsync()
    {
        _dotnetObjectReference = DotNetObjectReference.Create(this);
        People = GetPeople(100000);
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("UpdateWindowWidth", _dotnetObjectReference);
            await InitWindowWidthListener();
        }
    }

    private List<Person> GetPeople(int itemCount)
    {
        List<Person> people = new();

        for (int i = 0; i < itemCount; i++)
        {
            people.Add(new Person
            {
                Id = i + 1,
                FirstName = $"Person {i + 1}",
                LastName = $"Person Family {i + 1}",
                Job = $"Programmer {i + 1}"
            });
        }

        return people;
    }

    [JSInvokable]
    public void UpdateWindowWidth(int windowWidth)
    {
        WindowWidth = windowWidth;
        UpdateGridColumnCount(WindowWidth);
        StateHasChanged();
    }

    private async Task InitWindowWidthListener()
    {
        _resizeEventListenerId = Guid.NewGuid().ToString();
        await JSRuntime.InvokeVoidAsync("AddWindowWidthListener", _dotnetObjectReference, _resizeEventListenerId);
    }

    public void UpdateGridColumnCount(int windowWidth)
    {
        int spacing = 4;
        int actualItemSize = 150;
        int gridItemSize = actualItemSize + spacing;

        if (windowWidth > gridItemSize)
        {
            _gridColumnCount = windowWidth / gridItemSize;
            _virtualizeGridRef?.RefreshDataAsync();
        }

        StateHasChanged();
    }

    private async ValueTask<ItemsProviderResult<Person[]>> ProvideGridItemsAsync(ItemsProviderRequest request)
    {
        var cancellationToken = request.CancellationToken;
        if (cancellationToken.IsCancellationRequested) return default;

        var count = request.Count * _gridColumnCount;
        var start = request.StartIndex * _gridColumnCount;
        var requestCount = Math.Min(count, People.Count - start);

        var items = People.Skip(start).Take(requestCount).ToList();

        var result = items.Chunk(_gridColumnCount).ToList();

        return new ItemsProviderResult<Person[]>(items: result,
            totalItemCount: (int)Math.Ceiling((decimal)People.Count / _gridColumnCount));
    }
}
