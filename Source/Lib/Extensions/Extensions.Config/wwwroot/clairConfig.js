// The way JS Interop is done here is a bit outdated see export syntax:
// https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/

window.clairConfig = {
    altIsDown: false,
    controlTabChordIsInProgress: false,
	clickById: function (elementId) {
	    let element = document.getElementById(elementId);
        
        if (!element)
            return;
            
        element.click();
	},
	appWideKeyboardEventsInitialize: function (dotNetHelper) {
        document.body.addEventListener('keydown', (event) => {
            switch(event.key) {
                case "Shift":
                case "Meta":
                case "Control":
                    break;
                case "Alt":
                    if (window.clairConfig.altIsDown)
                        break;
                    window.clairConfig.altIsDown = true;
                    dotNetHelper.invokeMethodAsync("ReceiveOnKeyDown", event.key, event.shiftKey);
                    event.preventDefault();
                    break;
                case "Tab":
                    if (!event.ctrlKey)
                        break;
                    window.clairConfig.controlTabChordIsInProgress = true;
                    dotNetHelper.invokeMethodAsync("ReceiveOnKeyDown", event.key, event.shiftKey);
                    break;
                case "p":
                    if (!event.ctrlKey)
                        break;
                    dotNetHelper.invokeMethodAsync("ReceiveWidgetOnKeyDown");
                    break;
                case "s":
                    if (!event.ctrlKey)
                        break;
                    dotNetHelper.invokeMethodAsync("SaveFileOnKeyDown");
                    break;
                case "S":
                    if (!event.ctrlKey)
                        break;
                    dotNetHelper.invokeMethodAsync("SaveAllFileOnKeyDown");
                    break;
                case "F":
                    if (!event.ctrlKey)
                        break;
                    dotNetHelper.invokeMethodAsync("FindAllOnKeyDown");
                    break;
                case ",":
                    if (!event.ctrlKey)
                        break;
                    dotNetHelper.invokeMethodAsync("CodeSearchOnKeyDown");
                    break;
                case "f":
                    if (!event.altKey)
                        break;
                    window.clairConfig.clickById("ci_header-button-file");
                    break;
                case "t":
                    if (!event.altKey)
                        break;
                    window.clairConfig.clickById("ci_header-button-tools");
                    break;
                case "v":
                    if (!event.altKey)
                        break;
                    window.clairConfig.clickById("ci_header-button-view");
                    break;
                case "r":
                    if (!event.altKey)
                        break;
                    window.clairConfig.clickById("ci_header-button-run");
                    break;
                case "Escape":
                    dotNetHelper.invokeMethodAsync("EscapeOnKeyDown");
                    break;
                default:
                    break;
            }
            // event.preventDefault();
        });
        
        document.body.addEventListener('keyup', (event) => {
            switch(event.key) {
                case "Shift":
                case "Meta":
                case "Tab":
                    break;
                case "Control":
                    if (!window.clairConfig.controlTabChordIsInProgress)
                        break;
                    window.clairConfig.controlTabChordIsInProgress = false;
                    dotNetHelper.invokeMethodAsync("ReceiveOnKeyUp", event.key);
                    event.preventDefault();
                    break;
                case "Alt":
                    window.clairConfig.altIsDown = false;
                    dotNetHelper.invokeMethodAsync("ReceiveOnKeyUp", event.key);
                    event.preventDefault();
                    break;
                default:
                    break;
            }
            // event.preventDefault();
        });
        
        window.addEventListener('blur', function() {
          if (!window.clairConfig.altIsDown && !window.clairConfig.controlTabChordIsInProgress)
              return;
          window.clairConfig.altIsDown = false;
          window.clairConfig.controlTabChordIsInProgress = false;
          dotNetHelper.invokeMethodAsync("OnWindowBlur");
        });
    }
}
