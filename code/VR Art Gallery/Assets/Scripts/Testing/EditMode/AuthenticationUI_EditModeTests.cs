// Assets/Scripts/Testing/EditMode/AuthenticationUI_EditModeTests.cs
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRGallery.UI;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;


public class AuthenticationUI_EditModeTests
{
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(f, $"Field not found: {fieldName}");
        f.SetValue(obj, value);
    }

    private static object CallPrivate(object obj, string methodName, params object[] args)
    {
        var m = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(m, $"Method not found: {methodName}");
        return m.Invoke(obj, args);
    }

    private static TMP_InputField MakeTMPInputField(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var input = go.AddComponent<TMP_InputField>();

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();

        input.textComponent = text;
        input.text = "";
        return input;
    }

    private static TextMeshProUGUI MakeTMPText(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        return go.AddComponent<TextMeshProUGUI>();
    }

    private static Button MakeButton(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<Image>(); // Button typically expects a Graphic
        return go.AddComponent<Button>();
    }

    [Test]
    public void ShowAuthenticationUI_WhenNoAuthManager_ShowsLoginPanelAndClearsFields()
    {
        var root = new GameObject("AuthUIRoot");
        var ui = root.AddComponent<AuthenticationUI>();

        // Panels
        var loginPanel = new GameObject("LoginPanel");
        var registerPanel = new GameObject("RegisterPanel");
        var authedPanel = new GameObject("AuthedPanel");
        var loadingPanel = new GameObject("LoadingPanel");

        // Input fields
        var loginEmail = MakeTMPInputField("LoginEmail");
        var loginPass = MakeTMPInputField("LoginPass");

        // Set fields (private [SerializeField])
        SetPrivateField(ui, "loginPanel", loginPanel);
        SetPrivateField(ui, "registerPanel", registerPanel);
        SetPrivateField(ui, "authenticatedPanel", authedPanel);
        SetPrivateField(ui, "loadingPanel", loadingPanel);
        SetPrivateField(ui, "loginEmailField", loginEmail);
        SetPrivateField(ui, "loginPasswordField", loginPass);

        // Pre-fill to prove ClearInputFields is called
        loginEmail.text = "test@example.com";
        loginPass.text = "secret";

        ui.ShowAuthenticationUI(); // Should show login when authManager is null

        Assert.IsTrue(loginPanel.activeSelf);
        Assert.IsFalse(registerPanel.activeSelf);
        Assert.IsFalse(authedPanel.activeSelf);
        Assert.IsFalse(loadingPanel.activeSelf);

        Assert.AreEqual("", loginEmail.text);
        Assert.AreEqual("", loginPass.text);
    }

    [Test]
    public void LoginClick_WithInvalidInput_ShowsError()
    {
        var root = new GameObject("AuthUIRoot");
        var ui = root.AddComponent<AuthenticationUI>();

        // Setup panels
        var loginPanel = new GameObject("LoginPanel");
        var errorPanel = new GameObject("ErrorPanel");
        var loadingPanel = new GameObject("LoadingPanel");
        var errorText = MakeTMPText("ErrorText");

        // Setup input fields
        var loginEmail = MakeTMPInputField("LoginEmail");
        var loginPass = MakeTMPInputField("LoginPass");

        // Setup login button
        var loginButton = MakeButton("LoginButton");

        // Set private fields
        SetPrivateField(ui, "loginPanel", loginPanel);
        SetPrivateField(ui, "errorPanel", errorPanel);
        SetPrivateField(ui, "loadingPanel", loadingPanel);
        SetPrivateField(ui, "errorText", errorText);
        SetPrivateField(ui, "loginEmailField", loginEmail);
        SetPrivateField(ui, "loginPasswordField", loginPass);
        SetPrivateField(ui, "loginButton", loginButton);

        // Leave authManager null (don't set it)

        // Test Case 1: Empty email
        loginEmail.text = "";
        loginPass.text = "password123";

        LogAssert.Expect(LogType.Error, new Regex(@"\[AuthUI\] Please enter your email address"));

        CallPrivate(ui, "OnLoginClick");

        Assert.IsTrue(errorPanel.activeSelf);
        Assert.AreEqual("Please enter your email address", errorText.text);

        // Reset for Test Case 2
        errorPanel.SetActive(false);

        // Test Case 2: Empty password
        loginEmail.text = "test@example.com";
        loginPass.text = "";

        LogAssert.Expect(LogType.Error, new Regex(@"\[AuthUI\] Please enter your password"));

        CallPrivate(ui, "OnLoginClick");

        Assert.IsTrue(errorPanel.activeSelf);
        Assert.AreEqual("Please enter your password", errorText.text);

        // Cleanup
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(loginPanel);
        Object.DestroyImmediate(errorPanel);
        Object.DestroyImmediate(loadingPanel);
        Object.DestroyImmediate(loginEmail.gameObject);
        Object.DestroyImmediate(loginPass.gameObject);
        Object.DestroyImmediate(errorText.gameObject);
        Object.DestroyImmediate(loginButton.gameObject);
    }

    // ...existing code...

    [Test]
    public void LoginClick_WithValidInputs_AndNoAuthManager_ShouldNotThrow_ButShowError()
    {
        // Arrange: UI in scene, valid login inputs
        var root = new GameObject("AuthUIRoot");
        var ui = root.AddComponent<AuthenticationUI>();

        var loginPanel = new GameObject("LoginPanel");
        var loadingPanel = new GameObject("LoadingPanel");
        var errorPanel = new GameObject("ErrorPanel");
        var errorText = MakeTMPText("ErrorText");

        var loginEmail = MakeTMPInputField("LoginEmail");
        var loginPass = MakeTMPInputField("LoginPass");
        var loginButton = MakeButton("LoginButton");

        // Set valid input values after creation
        loginEmail.text = "someone@example.com";
        loginPass.text = "password123";

        SetPrivateField(ui, "loginPanel", loginPanel);
        SetPrivateField(ui, "loadingPanel", loadingPanel);
        SetPrivateField(ui, "loginEmailField", loginEmail);
        SetPrivateField(ui, "loginPasswordField", loginPass);
        SetPrivateField(ui, "loginButton", loginButton);
        SetPrivateField(ui, "errorPanel", errorPanel);
        SetPrivateField(ui, "errorText", errorText);

        errorPanel.SetActive(false);

        // Act + Assert: should NOT crash, should show auth-not-available error
        LogAssert.Expect(LogType.Error, new Regex(@"\[AuthUI\] Authentication system not available"));

        Assert.DoesNotThrow(() => CallPrivate(ui, "OnLoginClick"));

        Assert.IsTrue(errorPanel.activeSelf);
        Assert.AreEqual("Authentication system not available", errorText.text);

        // Cleanup
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(loginPanel);
        Object.DestroyImmediate(loadingPanel);
        Object.DestroyImmediate(errorPanel);
        Object.DestroyImmediate(loginEmail.gameObject);
        Object.DestroyImmediate(loginPass.gameObject);
        Object.DestroyImmediate(loginButton.gameObject);
        Object.DestroyImmediate(errorText.gameObject);

    }

    [Test]
    public void ValidateRegisterInput_InvalidEmail_ShowsErrorAndReturnsFalse()
    {
        var root = new GameObject("AuthUIRoot");
        var ui = root.AddComponent<AuthenticationUI>();

        var errorPanel = new GameObject("ErrorPanel");
        var errorText = MakeTMPText("ErrorText");

        var regEmail = MakeTMPInputField("RegEmail");
        var regPass = MakeTMPInputField("RegPass");
        var confirm = MakeTMPInputField("Confirm");

        SetPrivateField(ui, "errorPanel", errorPanel);
        SetPrivateField(ui, "errorText", errorText);

        SetPrivateField(ui, "registerEmailField", regEmail);
        SetPrivateField(ui, "registerPasswordField", regPass);
        SetPrivateField(ui, "confirmPasswordField", confirm);

        regEmail.text = "not-an-email";
        regPass.text = "123456";
        confirm.text = "123456";

        // Expect the log emitted by ShowError
        LogAssert.Expect(LogType.Error, new Regex(@"\[AuthUI\] Please enter a valid email address"));

        var ok = (bool)CallPrivate(ui, "ValidateRegisterInput");

        Assert.IsFalse(ok);
        Assert.IsTrue(errorPanel.activeSelf);
        Assert.AreEqual("Please enter a valid email address", errorText.text);
    }


}
