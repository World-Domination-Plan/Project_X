using NUnit.Framework;

public class UnityAuthTests
{
    [Test]
    public void PlayerId_Is_Stored_After_SignIn()
    {
        var auth = new UnityAuth();

        // Simulate what SignIn would do
        auth.SetPlayerIdForTest("test-player-id");

        Assert.IsFalse(string.IsNullOrEmpty(auth.PlayerId));
        Assert.AreEqual("test-player-id", auth.PlayerId);
    }
}
