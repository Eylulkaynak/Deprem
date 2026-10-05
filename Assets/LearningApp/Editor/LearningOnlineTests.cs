using System;
using System.IO;
using Deprem.Learning;
using NUnit.Framework;
using UnityEngine;

namespace Deprem.Learning.Tests
{
    public sealed class LearningOnlineTests
    {
        private string folder;
        [SetUp] public void Setup() { folder = Path.Combine(Path.GetTempPath(), "DepremOnlineTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); }
        [TearDown] public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        [Test] public void CredentialsSurviveRestartAndStaySeparateForEachProfileAndServer()
        {
            string path = Path.Combine(folder,"online.json");
            var store = new OnlineIdentityStore(path);
            string original = store.Identity("ada","https://games.example.com").credential;
            Assert.That(original.Length, Is.EqualTo(64));
            Assert.That(store.Identity("bora","https://games.example.com").credential, Is.Not.EqualTo(original));
            Assert.That(store.Identity("ada","https://other.example.com").credential, Is.Not.EqualTo(original));
            Assert.That(store.Save(), Is.True);
            Assert.That(new OnlineIdentityStore(path).Identity("ada","https://games.example.com").credential, Is.EqualTo(original));
        }
        [Test] public void DamagedOnlineSettingsRecoverIdentityFromBackup()
        {
            string path = Path.Combine(folder,"online.json");
            var store = new OnlineIdentityStore(path);
            string credential = store.Identity("ada","https://games.example.com").credential;
            store.Save(); store.Data.endpoint = "https://games.example.com"; store.Save();
            File.WriteAllText(path,"{broken");
            Assert.That(new OnlineIdentityStore(path).Identity("ada","https://games.example.com").credential, Is.EqualTo(credential));
        }
        [TestCase("https://games.example.com/",false,true)]
        [TestCase("http://127.0.0.1:8765",true,true)]
        [TestCase("http://games.example.com",false,false)]
        [TestCase("https://user:secret@games.example.com",true,false)]
        [TestCase("file:///tmp/data",true,false)]
        [TestCase("https://games.example.com?token=secret",true,false)]
        [TestCase("https://games.example.com/arbitrary-path",true,false)]
        public void EndpointValidationEnforcesBuildAndCredentialBoundaries(string address, bool development, bool expected)
        {
            Assert.That(LearningOnlineClient.ValidateEndpoint(address,development,out _), Is.EqualTo(expected));
        }
        [Test] public void EmptyServerRoomAndLeaderboardResultDoNotCreatePhantomObjects()
        {
            var state = JsonUtility.FromJson<OnlineSnapshot>("{\"me\":{\"id\":\"ada\"},\"room\":null,\"friends\":[],\"incoming\":[],\"outgoing\":[],\"invites\":[]}");
            state.Normalize(); Assert.That(state.room, Is.Null); Assert.That(state.friends, Is.Empty);
            var board = JsonUtility.FromJson<OnlineLeaderboard>("{\"entries\":[],\"self\":null}");
            board.Normalize(); Assert.That(board.self, Is.Null); Assert.That(board.entries, Is.Empty);
        }
        [Test] public void ServerSnapshotDeserializesPlayersRoundAndReconnectState()
        {
            var state = JsonUtility.FromJson<OnlineSnapshot>("{\"serverTime\":1791198000.5,\"me\":{\"id\":\"ada\"},\"room\":{\"id\":\"room\",\"state\":\"playing\",\"players\":[{\"id\":\"ada\",\"done\":2,\"eventSeq\":3,\"accepted\":[\"water\",\"radio\"]}],\"round\":{\"id\":\"catch-bag\",\"catchSpawns\":[{\"id\":\"water\",\"x\":0.75,\"at\":2.7,\"good\":true}]}}}");
            Assert.That(state.room.Player("ada").accepted.Length, Is.EqualTo(2));
            Assert.That(state.room.Player("ada").eventSeq, Is.EqualTo(3));
            Assert.That(state.room.round.catchSpawns[0].x, Is.EqualTo(.75f));
            Assert.That(state.room.Terminal, Is.False);
            state.room.state = "finished"; Assert.That(state.room.Terminal, Is.True);
        }
    }
}
