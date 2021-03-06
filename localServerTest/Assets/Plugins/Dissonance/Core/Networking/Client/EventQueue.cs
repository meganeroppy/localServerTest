﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Datastructures;
using Dissonance.Threading;

namespace Dissonance.Networking.Client
{
    internal class EventQueue
    {
        #region helper types
        private enum EventType
        {
            PlayerJoined,
            PlayerLeft,

            PlayerEnteredRoom,
            PlayerExitedRoom,

            PlayerStartedSpeaking,
            PlayerStoppedSpeaking,

            VoiceData,
            TextMessage
        }

        private struct NetworkEvent
        {
            public readonly EventType Type;

            private string _playerName;
            public string PlayerName
            {
                get
                {
                    return _playerName;
                }
                set
                {
                    _playerName = value;
                }
            }

            private string _room;
            public string Room
            {
                get
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    return _room;
                }
                set
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    _room = value;
                }
            }

            private ReadOnlyCollection<string> _allRooms;
            [NotNull] public ReadOnlyCollection<string> AllRooms
            {
                get
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    return _allRooms;
                }
                set
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    _allRooms = value;
                }
            }

            private readonly VoicePacket _voicePacket;
            public VoicePacket VoicePacket
            {
                get
                {
                    Check(EventType.VoiceData);
                    return _voicePacket;
                }
            }

            private readonly TextMessage _textMessage;
            public TextMessage TextMessage
            {
                get
                {
                    Check(EventType.TextMessage);
                    return _textMessage;
                }
            }

            #region constructors
            public NetworkEvent(EventType type)
            {
                Type = type;

                _playerName = null;
                _room = null;
                _allRooms = null;
                _voicePacket = default(VoicePacket);
                _textMessage = default(TextMessage);
            }

            public NetworkEvent(VoicePacket voice)
                : this(EventType.VoiceData)
            {
                _voicePacket = voice;
            }

            public NetworkEvent(TextMessage text)
                : this(EventType.TextMessage)
            {
                _textMessage = text;
            }
            #endregion

            #region accessor sanity checks
            private void Check(EventType type)
            {
                //This is a sanity check against developer mistakes. We can exclude it from final builds.
                #if UNITY_EDITOR
                    Log.AssertAndThrowPossibleBug(type == Type, "EA60F116-8B43-49B9-8625-2E19CF5137BD", "Attempted to access as {0}, but type is {1}", type, Type);
                #endif
            }

            private void Check(EventType typeA, EventType typeB)
            {
                //This is a sanity check against developer mistakes. We can exclude it from final builds.
                #if UNITY_EDITOR
                    Log.AssertAndThrowPossibleBug(
                        typeA == Type || typeB == Type,
                        "EA60F116-8B43-49B9-8625-2E19CF5137BD",
                        "Attempted to access as {0}|{1}, but type is {2}",
                        typeA, typeB, Type
                    );
                #endif
            }
            #endregion
        }
        #endregion

        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(EventQueue).Name);

        private readonly ReadonlyLockedValue<List<NetworkEvent>> _queuedEvents = new ReadonlyLockedValue<List<NetworkEvent>>(new List<NetworkEvent>());

        private readonly IRecycler<byte[]> _byteArrayPool;
        [NotNull] private readonly IRecycler<List<RemoteChannel>> _channelsListPool;

        public event Action<string> PlayerJoined;
        public event Action<string> PlayerLeft;
        public event Action<RoomEvent> PlayerEnteredRoom;
        public event Action<RoomEvent> PlayerExitedRoom;
        public event Action<VoicePacket> VoicePacketReceived;
        public event Action<TextMessage> TextMessageReceived;
        public event Action<string> PlayerStartedSpeaking;
        public event Action<string> PlayerStoppedSpeaking;

        internal event Action<string> OnEnqueuePlayerLeft;
        #endregion

        public EventQueue([NotNull]IRecycler<byte[]> byteArrayPool, [NotNull]IRecycler<List<RemoteChannel>> channelsListPool)
        {
            if (byteArrayPool == null) throw new ArgumentNullException("byteArrayPool");
            if (channelsListPool == null) throw new ArgumentNullException("channelsListPool");

            _byteArrayPool = byteArrayPool;
            _channelsListPool = channelsListPool;
        }

        /// <summary>
        /// Dispatch all events waiting in the queue to event handlers
        /// </summary>
        /// <remarks>Returns true if any invocation caused an error</remarks>
        public bool DispatchEvents()
        {
            var error = false;

            using (var events = _queuedEvents.Lock())
            {
                var queuedEvents = events.Value;

                for (var i = 0; i < queuedEvents.Count; i++)
                {
                    var e = queuedEvents[i];

                    switch (e.Type)
                    {
                        case EventType.PlayerJoined:
                            error |= InvokeEvent(e.PlayerName, PlayerJoined);
                            break;
                        case EventType.PlayerLeft:
                            error |= InvokeEvent(e.PlayerName, PlayerLeft);
                            break;
                        case EventType.PlayerStartedSpeaking:
                            error |= InvokeEvent(e.PlayerName, PlayerStartedSpeaking);
                            break;
                        case EventType.PlayerStoppedSpeaking:
                            error |= InvokeEvent(e.PlayerName, PlayerStoppedSpeaking);
                            break;
                        case EventType.VoiceData:
                            error |= InvokeEvent(e.VoicePacket, VoicePacketReceived);

                            //The voice packet event is special. It has some components which need to be recycled. Do that here
                            if (e.VoicePacket.Channels != null)
                            {
                                e.VoicePacket.Channels.Clear();
                                _channelsListPool.Recycle(e.VoicePacket.Channels);
                            }
                            _byteArrayPool.Recycle(e.VoicePacket.EncodedAudioFrame.Array);
                            break;
                        case EventType.TextMessage:
                            error |= InvokeEvent(e.TextMessage, TextMessageReceived);
                            break;
                        case EventType.PlayerEnteredRoom:
                            var evtEnter = CreateRoomEvent(e, true);
                            error |= InvokeEvent(evtEnter, PlayerEnteredRoom);
                            break;
                        case EventType.PlayerExitedRoom:
                            var evtExit = CreateRoomEvent(e, false);
                            error |= InvokeEvent(evtExit, PlayerExitedRoom);
                            break;

                        //ncrunch: no coverage start (Justification: It's a sanity check, we shouldn't ever hit this line)
                        default:
                            throw new ArgumentOutOfRangeException();
                        //ncrunch: no coverage end
                    }
                }

                queuedEvents.Clear();

                return error;
            }
        }

        private static RoomEvent CreateRoomEvent(NetworkEvent @event, bool joined)
        {
            return new RoomEvent
            {
                PlayerName = @event.PlayerName,
                Room = @event.Room,
                Joined = joined,
                Rooms = @event.AllRooms
            };
        }

        private static bool InvokeEvent<T>(T arg, [CanBeNull]Action<T> handler)
        {
            try
            {
                if (handler != null)
                    handler(arg);
            }
            catch (Exception e)
            {
                Log.Error("Exception invoking event handler: {0}", e);
                return true;
            }

            return false;
        }

        #region enqueue
        public void EnqueuePlayerJoined(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerJoined) { PlayerName = playerName });
        }

        public void EnqueuePlayerLeft(string playerName)
        {
            if (OnEnqueuePlayerLeft != null)
                OnEnqueuePlayerLeft(playerName);

            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerLeft) { PlayerName = playerName });
        }

        public void EnqueuePlayerEnteredRoom([NotNull] string playerName, [NotNull] string room, [NotNull, ItemNotNull] ReadOnlyCollection<string> allRooms)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerEnteredRoom) { PlayerName = playerName, Room = room, AllRooms = allRooms });
        }

        public void EnqueuePlayerExitedRoom([NotNull] string playerName, [NotNull] string room, [NotNull, ItemNotNull] ReadOnlyCollection<string> allRooms)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerExitedRoom) { PlayerName = playerName, Room = room, AllRooms = allRooms });
        }

        public void EnqueueStartedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStartedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueStoppedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStoppedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueVoiceData(VoicePacket data)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(data));
        }

        public void EnqueueTextData(TextMessage data)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(data));
        }
        #endregion
    }
}
