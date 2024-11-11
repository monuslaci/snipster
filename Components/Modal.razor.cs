using Microsoft.AspNetCore.Components;
using Snipster.Services;
using System;
using System.Reflection;
using static Snipster.Data.DBContext;


namespace Snipster.Components
{
    public partial class Modal
    {
        [Parameter] public string Title { get; set; } = "Modal Title";
        [Parameter] public RenderFragment ChildContent { get; set; }
        private bool IsVisible;

        public void ShowModal()
        {
            IsVisible = true;
            StateHasChanged();
        }

        public void CloseModal()
        {
            IsVisible = false;
            StateHasChanged();
        }
    }
}