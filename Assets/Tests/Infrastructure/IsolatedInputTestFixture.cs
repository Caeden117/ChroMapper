using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Tests.Infrastructure
{
    public class IsolatedInputTestFixture : InputTestFixture
    {
        private readonly List<InputAction> previouslyEnabledActions = new();

        public override void Setup()
        {
            previouslyEnabledActions.Clear();
            InputSystem.ListEnabledActions(previouslyEnabledActions);
            // Scene UI actions and standalone actions also retain device monitors across a runtime swap.
            // Suspend them before saving the original runtime, then restore their exact enabled state.
            foreach (var action in previouslyEnabledActions)
                action.Disable();

            base.Setup();

            // The runtime reset above drops project interaction registrations.
            StrobePulseWidthInput.RegisterInteraction();
        }

        public override void TearDown()
        {
            base.TearDown();
            foreach (var action in previouslyEnabledActions)
                action.Enable();

            previouslyEnabledActions.Clear();
        }
    }
}
