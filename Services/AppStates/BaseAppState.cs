using Microsoft.AspNetCore.Components;
using System;
using Snipster.Services;

namespace Snipster.Services.AppStates
{
    public class BaseAppState
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public event Action<ComponentBase, string> StateChanged;
        public void NotifyStateChanged(ComponentBase source, string property) => StateChanged?.Invoke(source, property);
    }
}
