﻿using System.Collections;
using System.Collections.Generic;
using DCL.Interface;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WorldChatWindowComponentViewShould
{
    private WorldChatWindowComponentView view;

    [SetUp]
    public void SetUp()
    {
        view = WorldChatWindowComponentView.Create();
        view.Initialize(Substitute.For<IChatController>(), Substitute.For<ILastReadMessagesService>());
    }

    [TearDown]
    public void TearDown()
    {
        view.Dispose();
    }

    [Test]
    public void Show()
    {
        view.Show();

        Assert.IsTrue(view.gameObject.activeSelf);
    }

    [Test]
    public void Hide()
    {
        view.Hide();

        Assert.IsFalse(view.gameObject.activeSelf);
    }

    [Test]
    public void ShowPrivateChatsLoading()
    {
        view.ShowPrivateChatsLoading();

        Assert.IsTrue(view.directChatsLoadingContainer.activeSelf);
        Assert.IsFalse(view.directChatsContainer.activeSelf);
        Assert.IsFalse(view.scroll.enabled);
    }

    [Test]
    public void HidePrivateChatsLoading()
    {
        view.HidePrivateChatsLoading();

        Assert.IsFalse(view.directChatsLoadingContainer.activeSelf);
        Assert.IsTrue(view.directChatsContainer.activeSelf);
        Assert.IsTrue(view.scroll.enabled);
    }

    [UnityTest]
    public IEnumerator CreatePrivateChat()
    {
        const string userId = "userId";
        GivenPrivateChat(userId);

        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(1, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());

        // direct chats are enqueued, then created in the next frame
        yield return null;

        Assert.AreEqual(1, view.directChatList.Count());
        Assert.AreEqual(1, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());
        Assert.AreEqual("Direct Messages (1)", view.directChatsHeaderLabel.text);
        Assert.AreEqual("Results (0)", view.searchResultsHeaderLabel.text);
        Assert.IsNotNull(view.directChatList.Get(userId));
    }

    [UnityTest]
    public IEnumerator CreateManyPrivateChats()
    {
        GivenPrivateChat("pepe");
        GivenPrivateChat("bleh");
        
        yield return null;
        
        Assert.AreEqual(2, view.directChatList.Count());
        Assert.AreEqual(2, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());
        Assert.AreEqual("Direct Messages (2)", view.directChatsHeaderLabel.text);
        Assert.AreEqual("Results (0)", view.searchResultsHeaderLabel.text);
        Assert.IsNotNull(view.directChatList.Get("pepe"));
        Assert.IsNotNull(view.directChatList.Get("bleh"));
    }

    [UnityTest]
    public IEnumerator RemovePrivateChat()
    {
        const string userId = "userId";

        GivenPrivateChat(userId);

        yield return null;

        view.RemovePrivateChat(userId);

        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(0, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());
        Assert.IsNull(view.directChatList.Get(userId));
    }

    [UnityTest]
    public IEnumerator ReplacePrivateChat()
    {
        const string userId = "userId";
        var profile = ScriptableObject.CreateInstance<UserProfile>();
        profile.UpdateData(new UserProfileModel
        {
            userId = userId,
            name = "userName"
        });

        var model = new PrivateChatModel
        {
            user = profile,
            isBlocked = false,
            isOnline = false,
            recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "hello!")
        };

        view.SetPrivateChat(model);

        model.recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "bleh");
        view.SetPrivateChat(model);

        yield return null;

        model.recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "foo");
        view.SetPrivateChat(model);

        yield return null;

        Assert.AreEqual(1, view.directChatList.Count());
        Assert.AreEqual(1, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());
        Assert.AreEqual("Direct Messages (1)", view.directChatsHeaderLabel.text);
        Assert.AreEqual("Results (0)", view.searchResultsHeaderLabel.text);
        Assert.IsNotNull(view.directChatList.Get(userId));
    }

    [Test]
    public void CreatePublicChannel()
    {
        const string channelId = "general";

        GivenPublicChannel(channelId, "nearby");

        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(0, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(1, view.publicChannelList.Count());
        Assert.IsNotNull(view.publicChannelList.Get(channelId));
    }
    
    [Test]
    public void CreateManyPublicChannels()
    {
        GivenPublicChannel("nearby", "nearby");
        GivenPublicChannel("nfts", "nfts");

        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(0, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(2, view.publicChannelList.Count());
        Assert.IsNotNull(view.publicChannelList.Get("nearby"));
        Assert.IsNotNull(view.publicChannelList.Get("nfts"));
    }

    [UnityTest]
    public IEnumerator ReplacePublicChannel()
    {
        const string channelId = "general";

        var model = new PublicChatChannelModel(channelId, "nearby", "any description");
        view.SetPublicChannel(model);

        yield return null;

        model.name = "hoho";
        view.SetPublicChannel(model);

        yield return null;

        model.description = "another description";
        view.SetPublicChannel(model);

        yield return null;

        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(0, view.PrivateChannelsCount);
        Assert.AreEqual(0, view.searchResultsList.Count());
        Assert.AreEqual(1, view.publicChannelList.Count());
        Assert.IsNotNull(view.publicChannelList.Get(channelId));
    }

    [Test]
    public void TriggerOpenPublicChannel()
    {
        const string expectedChannelId = "general";
        var channelId = "";
        view.OnOpenPublicChannel += s => channelId = s;
        GivenPublicChannel(expectedChannelId, "nearby");

        view.publicChannelList.Get(expectedChannelId).openChatButton.onClick.Invoke();

        Assert.AreEqual(expectedChannelId, channelId);
    }

    [UnityTest]
    public IEnumerator TriggerOpenPrivateChannel()
    {
        const string userId = "userId";
        var channelId = "";
        view.OnOpenPrivateChat += s => channelId = s;
        GivenPrivateChat(userId);

        yield return null;

        view.directChatList.Get(userId).openChatButton.onClick.Invoke();

        Assert.AreEqual(userId, channelId);
    }

    [Test]
    public void TriggerClose()
    {
        var closed = false;
        view.OnClose += () => closed = true;

        view.closeButton.onClick.Invoke();

        Assert.IsTrue(closed);
    }

    [Test]
    public void TriggerSearchChannel()
    {
        var search = "";
        view.OnSearchChannelRequested += s => search = s;

        view.searchBar.SubmitSearch("hello");

        Assert.AreEqual("hello", search);
    }

    [Test]
    public void TriggerRequestMorePrivateChats()
    {
        var called = false;
        view.OnRequireMorePrivateChats += () => called = true;

        view.loadMoreEntriesButton.onClick.Invoke();

        Assert.IsTrue(called);
    }

    [Test]
    public void UpdateHeadersOnStart()
    {
        Assert.AreEqual("Direct Messages (0)", view.directChatsHeaderLabel.text);
        Assert.AreEqual("Results (0)", view.searchResultsHeaderLabel.text);
    }

    [Test]
    public void ShowMoreChatsToLoadHint()
    {
        view.ShowMoreChatsToLoadHint(5);

        Assert.AreEqual("5 chats hidden. Use the search bar to find them or click below to show more.",
            view.loadMoreEntriesLabel.text);
        Assert.IsTrue(view.loadMoreEntriesContainer.activeSelf);
    }

    [Test]
    public void HideMoreChatsToLoadHint()
    {
        view.HideMoreChatsToLoadHint();

        Assert.IsFalse(view.loadMoreEntriesContainer.activeSelf);
    }

    [UnityTest]
    public IEnumerator Filter()
    {
        GivenPrivateChat("pepe");
        GivenPrivateChat("genio");
        GivenPrivateChat("bleh");
        GivenPublicChannel("general", "general");

        yield return null;

        view.Filter(new Dictionary<string, PrivateChatModel>
        {
            {
                "genio", new PrivateChatModel
                {
                    user = GivenProfile("genio"),
                    recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "hello")
                }
            },
            {
                "pepe", new PrivateChatModel
                {
                    user = GivenProfile("pepe"),
                    recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "buy my nft")
                }
            }
        }, new Dictionary<string, PublicChatChannelModel>
        {
            {
                "general", new PublicChatChannelModel("general", "general", "")
            }
        });

        yield return null;
        
        Assert.AreEqual(0, view.directChatList.Count());
        Assert.AreEqual(0, view.PrivateChannelsCount);
        Assert.AreEqual(3, view.searchResultsList.Count());
        Assert.AreEqual(0, view.publicChannelList.Count());
        Assert.AreEqual("Results (3)", view.searchResultsHeaderLabel.text);
        Assert.IsNotNull(view.searchResultsList.Get("genio"));
        Assert.IsNotNull(view.searchResultsList.Get("pepe"));
        Assert.IsNotNull(view.searchResultsList.Get("general"));
        Assert.IsTrue(view.searchResultsHeader.activeSelf);
        Assert.IsTrue(view.searchResultsList.isVisible);
        Assert.IsFalse(view.directChatsContainer.activeSelf);
        Assert.IsFalse(view.directChannelHeader.activeSelf);
        Assert.IsFalse(view.directChatList.isVisible);
        Assert.IsFalse(view.publicChannelList.isVisible);
    }

    [UnityTest]
    public IEnumerator ClearFilter()
    {
        yield return Filter();
        
        view.ClearFilter();
        
        Assert.AreEqual(3, view.directChatList.Count());
        Assert.AreEqual(3, view.PrivateChannelsCount);
        Assert.AreEqual(1, view.publicChannelList.Count());
        Assert.AreEqual("Direct Messages (3)", view.directChatsHeaderLabel.text);
        Assert.IsNotNull(view.directChatList.Get("genio"));
        Assert.IsNotNull(view.directChatList.Get("pepe"));
        Assert.IsNotNull(view.directChatList.Get("bleh"));
        Assert.IsNotNull(view.publicChannelList.Get("general"));
        Assert.IsFalse(view.searchResultsHeader.activeSelf);
        Assert.IsFalse(view.searchResultsList.isVisible);
        Assert.IsTrue(view.directChatsContainer.activeSelf);
        Assert.IsTrue(view.directChannelHeader.activeSelf);
        Assert.IsTrue(view.directChatList.isVisible);
        Assert.IsTrue(view.publicChannelList.isVisible);
    }

    [UnityTest]
    public IEnumerator SearchAndClearManyTimes()
    {
        yield return Filter();
        yield return ClearFilter();
        yield return Filter();
        yield return ClearFilter();
    }

    private void GivenPrivateChat(string userId)
    {
        var profile = GivenProfile(userId);

        view.SetPrivateChat(new PrivateChatModel
        {
            user = profile,
            isBlocked = false,
            isOnline = false,
            recentMessage = new ChatMessage(ChatMessage.Type.PRIVATE, "senderId", "hello!")
        });
    }

    private void GivenPublicChannel(string channelId, string name)
    {
        view.SetPublicChannel(new PublicChatChannelModel(channelId, name, "any description"));
    }

    private UserProfile GivenProfile(string userId)
    {
        var profile = ScriptableObject.CreateInstance<UserProfile>();
        profile.UpdateData(new UserProfileModel
        {
            userId = userId,
            name = "userName"
        });
        return profile;
    }
}