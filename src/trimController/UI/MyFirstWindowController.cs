using KSP.UI.Binding;
using trimController.Unity.Runtime;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;
using KSP;
using KSP.Sim.impl;
using KSP.Game;
using Newtonsoft.Json;

namespace trimController.UI;

/// <summary>
/// Controller for the MyFirstWindow UI.
/// </summary>
public class MyFirstWindowController : MonoBehaviour
{
    // The UIDocument component of the window game object
    private UIDocument _window;

    // The elements of the window that we need to access
    private VisualElement _rootElement;

    private Slider _Yaw_Slider; // this is the slider for the yaw trim
    private Slider _Pitch_Slider; // this is the slider for the pitch trim
    private Slider _Roll_Slider; // this is the slider for the roll trim

    private Button _Yaw_Up; // this is the button to increase the yaw trim
    private Button _Yaw_Down; // this is the button to decrease the yaw trim
    private Button _Yaw_Reset; // this is the button to reset the yaw trim

    private Button _Pitch_Up; // this is the button to increase the pitch trim
    private Button _Pitch_Down; // this is the button to decrease the pitch trim
    private Button _Pitch_Reset; // this is the button to reset the pitch trim

    private Button _Roll_Up; // this is the button to increase the roll trim
    private Button _Roll_Down; // this is the button to decrease the roll trim
    private Button _Roll_Reset; // this is the button to reset the roll trim

    private VesselVehicle thisvesselInstance;

    // The backing field for the IsWindowOpen property
    private bool _isWindowOpen;

    /// <summary>
    /// The state of the window. Setting this value will open or close the window.
    /// </summary>
    public bool IsWindowOpen
    {
        get => _isWindowOpen;
        set
        {
            _isWindowOpen = value;

            // Set the display style of the root element to show or hide the window
            _rootElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            // Alternatively, you can deactivate the window game object to close the window and stop it from updating,
            // which is useful if you perform expensive operations in the window update loop. However, this will also
            // mean you will have to re-register any event handlers on the window elements when re-enabled in OnEnable.
            // gameObject.SetActive(value);

            // YawUpdate the Flight AppBar button state
            GameObject.Find(trimControllerPlugin.ToolbarFlightButtonID)
                ?.GetComponent<UIValue_WriteBool_Toggle>()
                ?.SetValue(value);

            // YawUpdate the OAB AppBar button state
            GameObject.Find(trimControllerPlugin.ToolbarOabButtonID)
                ?.GetComponent<UIValue_WriteBool_Toggle>()
                ?.SetValue(value);
        }
    }

    /// <summary>
    /// Runs when the window is first created, and every time the window is re-enabled.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log("UI enabled");
        // Get the UIDocument component from the game object
        _window = GetComponent<UIDocument>();

        // Get the root element of the window.
        // Since we're cloning the UXML tree from a VisualTreeAsset, the actual root element is a TemplateContainer,
        // so we need to get the first child of the TemplateContainer to get our actual root VisualElement.
        _rootElement = _window.rootVisualElement[0];
        _Yaw_Slider = _rootElement.Q<Slider>("Yaw-Slider");
        _Pitch_Slider = _rootElement.Q<Slider>("Pitch-Slider");
        _Roll_Slider = _rootElement.Q<Slider>("Roll-Slider");

        _Yaw_Up = _rootElement.Q<Button>("yawPlus");
        _Yaw_Down = _rootElement.Q<Button>("yawMinus");
        _Yaw_Reset = _rootElement.Q<Button>("yawReset");

        _Pitch_Up = _rootElement.Q<Button>("pitchPlus");
        _Pitch_Down = _rootElement.Q<Button>("pitchMinus");
        _Pitch_Reset = _rootElement.Q<Button>("pitchReset");

        _Roll_Up = _rootElement.Q<Button>("rollPlus");
        _Roll_Down = _rootElement.Q<Button>("rollMinus");
        _Roll_Reset = _rootElement.Q<Button>("rollReset");

        // Center the window by default
        _rootElement.CenterByDefault();

        // Get the close button from the window
        var closeButton = _rootElement.Q<Button>("close-button");
        // Add a click event handler to the close button
        closeButton.clicked += () => IsWindowOpen = false;

        var currentYawSliderValue = _Yaw_Slider.value;
        var currentPitchSliderValue = _Pitch_Slider.value;
        var currentRollSliderValue = _Roll_Slider.value;

        _Yaw_Slider.RegisterValueChangedCallback(e => controlUpdate(e.newValue, "yawTrim", false));
        _Pitch_Slider.RegisterValueChangedCallback(e => controlUpdate(e.newValue, "pitchTrim", false));
        _Roll_Slider.RegisterValueChangedCallback(e => controlUpdate(e.newValue, "rollTrim", false));

        _Yaw_Up.clicked += () => controlUpdate(0.1f, "yawTrim", true);
        _Yaw_Down.clicked += () => controlUpdate(-0.1f, "yawTrim", true);
        _Yaw_Reset.clicked += () => controlUpdate(0.0f, "yawTrim", false);

        _Pitch_Up.clicked += () => controlUpdate(0.1f, "pitchTrim", true);
        _Pitch_Down.clicked += () => controlUpdate(-0.1f, "pitchTrim", true);
        _Pitch_Reset.clicked += () => controlUpdate(0.0f, "pitchTrim", false);

        _Roll_Up.clicked += () => controlUpdate(0.1f, "rollTrim", true);
        _Roll_Down.clicked += () => controlUpdate(-0.1f, "rollTrim", true);
        _Roll_Reset.clicked += () => controlUpdate(0.0f, "rollTrim", false);
    }

    // a function to take a float value and a control name and set the control value

    private void controlUpdate(float val, string controlName, bool step)
    {
        /// step is a boolean to indicate if the value should be increased by val or set to val
        /// val is the value to set the control to
        /// controlName is the name of the control to set
        
        var slider_value = val;
        if (step)
        {
            if (controlName == "yawTrim")
            {
                slider_value = _Yaw_Slider.value + val;
            }
            if (controlName == "pitchTrim")
            {
                slider_value = _Pitch_Slider.value + val;
            }
            if (controlName == "rollTrim")
            {
                slider_value = _Roll_Slider.value + val;
            }
        }
        if (controlName == "yawTrim")
        {
            _Yaw_Slider.value = slider_value;
        }
        if (controlName == "pitchTrim")
        {
            _Pitch_Slider.value = slider_value;
        }
        if (controlName == "rollTrim")
        {
            _Roll_Slider.value = slider_value;
        }

        // update the slider value

        if (GameManager.Instance?.Game?.ViewController?.GetActiveVehicle() != null)
        {
            GameManager.Instance.Game.ViewController.TryGetActiveVehicle(out var vessel);
            if (vessel != null)
            {
                var thisvessel = vessel as VesselVehicle;
                thisvesselInstance = thisvessel;
                controlSet(thisvesselInstance, controlName, slider_value);
            }
        }
    }

    private void Update()
    // if the window is close, break

    {
        if (!IsWindowOpen)
        {
            return;
        } 
        // Update the sliders to reflect the current trim values
        if (GameManager.Instance?.Game?.ViewController?.GetActiveVehicle() != null)
        {
            GameManager.Instance.Game.ViewController.TryGetActiveVehicle(out var vessel);
            if (vessel != null)
            {
                var thisvessel = vessel as VesselVehicle;
                thisvesselInstance = thisvessel;
                _Yaw_Slider.value = thisvesselInstance.yawTrim;
                _Pitch_Slider.value = thisvesselInstance.pitchTrim;
                _Roll_Slider.value = thisvesselInstance.rollTrim;
            }
        }
    }


    private void controlSet(VesselVehicle vessel, string controlName, float value)
    {
        if (vessel == null)
        {
            Debug.Log("vessel is Null");
            return;
        }
        // this is a helper function to set a control value on a vessel
        //public void AtomicSet(float? mainThrottle, float? roll, float? yaw, float? pitch, float? rollTrim, float? yawTrim, float? pitchTrim, float? wheelSteer, float? wheelSteerTrim, float? wheelThrottle, float? wheelThrottleTrim, bool? killRot, bool? gearUp, bool? gearDown, bool? headlight)
        switch (controlName)
        {
            case "mainThrottle":
                vessel.AtomicSet(value, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
                break;

            case "roll":
                vessel.AtomicSet(null, value, null, null, null, null, null, null, null, null, null, null, null, null, null);
                break;

            case "yaw":
                vessel.AtomicSet(null, null, value, null, null, null, null, null, null, null, null, null, null, null, null);
                break;

            case "pitch":
                vessel.AtomicSet(null, null, null, value, null, null, null, null, null, null, null, null, null, null, null);
                break;

            case "rollTrim":
                vessel.AtomicSet(null, null, null, null, value, null, null, null, null, null, null, null, null, null, null);
                break;

            case "yawTrim":
                vessel.AtomicSet(null, null, null, null, null, value, null, null, null, null, null, null, null, null, null);
                break;

            case "pitchTrim":
                vessel.AtomicSet(null, null, null, null, null, null, value, null, null, null, null, null, null, null, null);
                break;

            case "wheelSteer":
                vessel.AtomicSet(null, null, null, null, null, null, null, value, null, null, null, null, null, null, null);
                break;

            case "wheelSteerTrim":
                vessel.AtomicSet(null, null, null, null, null, null, null, null, value, null, null, null, null, null, null);
                break;

            case "wheelThrottle":
                vessel.AtomicSet(null, null, null, null, null, null, null, null, null, value, null, null, null, null, null);
                break;

            case "wheelThrottleTrim":
                vessel.AtomicSet(null, null, null, null, null, null, null, null, null, null, value, null, null, null, null);
                break;

            default:
                break;
        }
    }
}