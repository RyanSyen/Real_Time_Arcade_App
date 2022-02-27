using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Assignment
{
    public class AppHub : Hub
    {
        public class Point 
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        protected class Customer 
        {
            public string Username     { get; }
            public string ConnectionId { get; }
            public string Photo        { get; }

            public Customer(string username, string connectionId, string photo) {
                Username     = username;
                ConnectionId = connectionId;
                Photo        = photo;
            }
        }

        protected class Group 
        {
            public string                  GroupId            { get; }        // Group Id
            public Queue<string>           DrawerQueue        { get; set; }   // To determine which is the drawer next
            public Dictionary<string, int> UserScore          { get; set; }   // To count the score of the user
            public string                  Host               { get; set; }   // To check who is the host user
            public bool                    IsPublicAccess     { get; set; }   // To check if the room is accessible from public
            public string                  Status             { get; set; }   // To know the current state
            public string                  DrawerNow          { get; set; }   // Get the Drawer
            public string                  WordsNow           { get; set; }   // The current guess word
            public List<string>            Words              { get; set; }   // The words database
            public List<string>            WordsLeft          { get; set; }   // The words left to used
            public DateTime                ProgressStartTime  { get; set; }   // The progress start time for calculating
            public bool                    DrawerDisconnected { get; set; }   // to detect if drawer disconnected
            public Dictionary<string, int> DrawerScoreHistory { get; set; }   // To Record down the added score history
            public int                     MarkSequence       { get; set; }   // To mark accordingly during drawing state
            public List<string>            ReportDrawerUser   { get; set; }   // count how many report for drawer
            public string                  CanvasURL          { get; set; }   // For loading canvas

            public Group(bool isPublicAccess, string groupId) 
            {
                GroupId        = groupId;
                DrawerQueue    = new Queue<string>();
                UserScore      = new Dictionary<string, int>();
                Host           = "";
                IsPublicAccess = isPublicAccess;
                Status         = "Ready";
                DrawerNow      = "";
                WordsNow       = "";
                Words = new List<string>() {
                    "lamp",
                    "table",
                    "fire fighter",
                    "monitor",
                    "eye",
                    "pillow",
                    "calculator",
                    "laptop",
                    "dustbin",
                    "chicken",
                    "bag",
                    "zebra",
                    "glass"
                };
                WordsLeft = new List<string>(Words);
                DrawerScoreHistory = new Dictionary<string, int>();
                MarkSequence = 11;
                ReportDrawerUser = new List<string>();
                CanvasURL = null;
            }

            // Add member to this group
            public void AddMember(string username) 
            {
                DrawerQueue.Enqueue(username); // Queue the drawer
                UserScore.Add(username, 0);    // Add the user to the list

                // if it is the first user, he is the host
                if (UserScore.Count == 1) 
                {
                    Host = username;
                }
            }

            // Remove member from the group
            public void RemoveMember(string username) 
            {
                // Remove all the things from this user
                UserScore.Remove(username);
                DrawerQueue = new Queue<string>(DrawerQueue.Where(x => x != username));
                DrawerScoreHistory.Remove(username);
                ReportDrawerUser.Remove(username);

                // if the group was not empty, and the host left, then assign a host to someone
                if (UserScore.Count > 0 && Host == username) 
                {
                    // Assign a host to someone randomly in the group
                    var random = new Random();
                    int index = random.Next(UserScore.Count);
                    Host = UserScore.ElementAt(index).Key;
                }
            }

            // Dequeue and enqueue the drawer again, and return drawer name
            public string QueueDrawing() 
            {
                DrawerNow = DrawerQueue.Dequeue();
                DrawerQueue.Enqueue(DrawerNow);
                return DrawerNow;
            }

            // Randomly pick words from database
            public string TakeWords() 
            {
                // if words has been used up, refresh the words
                if (WordsLeft.Count == 0) {
                    WordsLeft = new List<string>(Words);
                }

                // Assign a random List of words
                var random  = new Random();
                int index   = random.Next(WordsLeft.Count);
                WordsNow    = WordsLeft[index];

                // remove the words from wordsLeft
                WordsLeft.Remove(WordsNow);

                return WordsNow;
            }

            // Reset The Game
            public void ResetGame() 
            {
                Status = "Ready";
                DrawerNow = "";
                WordsNow = "";
                WordsLeft = new List<string>(Words);
                UserScore = UserScore.ToDictionary(p => p.Key, p => 0);
                CanvasURL = null;
                ResetScore();
            }

            public int GetMemberCount() 
            {
                return UserScore.Count;
            }

            public void AddScoreHistory(string username) 
            {
                if (MarkSequence == 11) {
                    // if mark is start at 11, give mark to drawer
                    DrawerScoreHistory.Add(DrawerNow, MarkSequence);
                    UserScore[DrawerNow] += MarkSequence;
                    MarkSequence--;
                }
                else {
                    // else, add 2 mark only
                    DrawerScoreHistory[DrawerNow] += 2;
                    UserScore[DrawerNow] += 2;
                }

                // others will have consequence mark
                DrawerScoreHistory.Add(username, MarkSequence);
                UserScore[username] += MarkSequence;
                MarkSequence--;
            }

            // Reset the score add for current round
            public void ResetScore() 
            {
                MarkSequence = 11;
                DrawerScoreHistory.Clear();
                ReportDrawerUser.Clear();
            }

            // if all guesser have guessed correct
            public bool AllUserGuessedCorrect() {
                return UserScore.Count == DrawerScoreHistory.Count;
            }

            public void RedoScore() {
                foreach (var history in DrawerScoreHistory)
                {
                    UserScore[history.Key] -= history.Value;
                }
            }

            // Add Report Count
            public void AddReportDrawerCount(string username) {
                // if it does not exist in the report drawer
                // and it was not drawer
                if (!ReportDrawerUser.Contains(username) && DrawerNow != username) {
                    // add to ReportDrawerlist
                    ReportDrawerUser.Add(username); 
                }
            }

            // To know how many report made to the drawer
            public int GetReportDrawerCount() {
                return ReportDrawerUser.Count;
            }

            // if only 2 member left, could not report drawer
            // if member exceed 2, check if all the guesser agree to report drawer
            public bool ReportDrawerCountExceeded() {
                if (GetMemberCount() == 2) {
                    return false;
                }
                else if (GetMemberCount() >= 3 && GetMemberCount() <= 4) {
                    return (ReportDrawerUser.Count >= 2);
                }
                else if (GetMemberCount() == 5) {
                    return (ReportDrawerUser.Count >= 3);
                }

                return false;
            }
        }


        private static ConcurrentDictionary<string, Customer> cd = new ConcurrentDictionary<string, Customer>();    // To Get All the users
        private static bool isDuplicate = false;                                                                    // To check is the user got duplicate name

        private static ConcurrentDictionary<string, Group> groupList = new ConcurrentDictionary<string, Group>();   // To Get All the game grouplist
        // All the state miliseconds
        private static int DrawingReadyTime     = 5000;
        private static int DrawingLosesTurnTime = 5000;
        private static int DrawingTime          = 50000;
        private static int DrawingSkippedTime   = 5000;
        private static int DrawingReportedTime  = 5000;
        private static int AnswerTime           = 5000;
        private static int AnswerCorrectTime    = 5000;
        private static int WinnerTime           = 10000;

        // The maximum score to win
        private static int ScoreMaximum = 70;

        // ===================================================================================================
        public override async Task OnConnectedAsync()
        {
            // username querystring
            string username = Context.GetHttpContext().Request.Query["username"];
            string photo = Context.GetHttpContext().Request.Query["photo"];
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            // if duplicate name found, redirect back to home page.
            // and do nothing
            if (!cd.TryAdd(username, new Customer(username, Context.ConnectionId, photo))) 
            {
                isDuplicate = true;
                await Clients.Caller.SendAsync("DuplicateUser");
                return;
            } 
            else if (!String.IsNullOrEmpty(groupId) && !groupList.ContainsKey(groupId)) 
            {
                // Grouping
                // If user was in game.html page, and Group id is invalid, then display error message
                // and do nothing
                await Clients.Caller.SendAsync("NotAllowedEnter", "Invalid Group Id");
                return;
            }
            else if (!String.IsNullOrEmpty(groupId) && groupList[groupId].GetMemberCount() >= 5) 
            {
                // Grouping
                // if user was in game.html page, and the room is full, display room full error message
                // and do nothing
                await Clients.Caller.SendAsync("NotAllowedEnter", "Room Full");
                return;
            }
            
            // for grouping adding
            // if it was in game.html page, then add the username to the group list
            if (!String.IsNullOrEmpty(groupId)) 
            {
                // Take the specific group database finding by group id  
                Group group = groupList[groupId];                                    

                group.AddMember(username);                                           // add the username to the group
                await Groups.AddToGroupAsync(cd[username].ConnectionId, groupId);    // add the user connection id to the signalr group

                // Append the Game Room at room.html, or update the game room, if the room is public accessible
                if (group.IsPublicAccess) {
                    await Clients.Others.SendAsync("AppendGameRoom", groupId, group.GetMemberCount());
                }

                // Refresh the user list
                await RefreshUserList(groupId);
                // display user join message
                await Clients.Group(groupId).SendAsync("UserJoinGame", username);

                // Get the progress time
                DateTime currentDateTime = DateTime.Now;
                TimeSpan span = currentDateTime - group.ProgressStartTime;
                int ms = (int)span.TotalMilliseconds;

                // this is for syncing the progress bar
                string percentage;
                int timeTaken;

                // To display drawed canvas
                await Clients.Caller.SendAsync("ReceiveImageData", group.CanvasURL);

                // if it was the ready state
                if (group.Status == "Ready") {
                    // if the user is a host
                    if (group.Host == username) 
                    {
                        // display waiting other player message
                        await Clients.Caller.SendAsync("WaitingPlayerStatus");
                    } 
                    else {
                        // if the user is not host, display waiting for host to start message
                        await Clients.Caller.SendAsync("WaitingHostStatus");
                        // and then let the host display host start button
                        await Clients.Client(cd[group.Host].ConnectionId).SendAsync("HostStartStatus");
                    }
                }
                else if (group.Status == "DrawingReady") {
                    percentage = GetPercentage(ms, DrawingReadyTime);
                    timeTaken = DrawingReadyTime - ms;
                    await Clients.Caller.SendAsync("WhoTurnDrawingStatus", group.DrawerNow, timeTaken, percentage);
                }
                else if (group.Status == "DrawingLosesTurn") {
                    percentage = GetPercentage(ms, DrawingLosesTurnTime);
                    timeTaken = DrawingLosesTurnTime - ms;
                    await Clients.Caller.SendAsync("DrawingLosesTurnStatus", group.DrawerNow, timeTaken, percentage);
                }
                else if (group.Status == "Drawing") {
                    percentage = GetPercentage(ms, DrawingTime);
                    timeTaken = DrawingTime - ms;
                    await Clients.Caller.SendAsync("WhoDrawingStatus", group.DrawerNow, timeTaken, percentage);
                }
                else if (group.Status == "DrawingReported") {
                    percentage = GetPercentage(ms, DrawingReportedTime);
                    timeTaken = DrawingReportedTime - ms;
                    await Clients.Caller.SendAsync("DrawingReportedStatus", group.DrawerNow, timeTaken, percentage);
                }
                else if (group.Status == "DrawingSkipped") {
                    percentage = GetPercentage(ms, DrawingSkippedTime);
                    timeTaken = DrawingSkippedTime - ms;
                    await Clients.Caller.SendAsync("DrawingSkippedStatus", group.DrawerNow, timeTaken, percentage);
                }
                else if (group.Status == "Answer") {
                    percentage = GetPercentage(ms, AnswerTime);
                    timeTaken = AnswerTime - ms;
                    await Clients.Caller.SendAsync("AnswerStatus", group.WordsNow, timeTaken, percentage);
                }
                else if (group.Status == "AnswerCorrect") {
                    percentage = GetPercentage(ms, AnswerCorrectTime);
                    timeTaken = AnswerCorrectTime - ms;
                    await Clients.Caller.SendAsync("AnswerCorrectStatus", group.WordsNow, timeTaken, percentage);
                }
                else if (group.Status == "Winner") {
                    percentage = GetPercentage(ms, WinnerTime);
                    timeTaken = WinnerTime - ms;

                    // get the Top Three User
                    var topThreeUser = group.UserScore.OrderByDescending(x => x.Value)
                                                    .Select(x => x.Key)
                                                    .Take(3)
                                                    .ToList();
                    await Clients.Caller.SendAsync("WinnerStatus", topThreeUser, timeTaken, percentage);
                }
            }
            // show container
            // both room.html and game.html have container div for displaying content
            await Clients.Caller.SendAsync("ShowContainer");

            // Show all user chat for caller
            await Clients.Caller.SendAsync("ShowAllUserChat", cd);
            // Append the user chat for others
            await Clients.Others.SendAsync("AppendUserChat", cd[username]);

            // No idea what is this for...
            await base.OnConnectedAsync();
        }

        // ===================================================================================================
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string username = Context.GetHttpContext().Request.Query["username"];       // username querystring
            string groupId  = Context.GetHttpContext().Request.Query["groupId"];
            bool   isDrawerDisconnected = false;

            // if duplicate, dont have to do anything
            if (isDuplicate) {
                // Set Back to false
                isDuplicate = false;
                // No idea what is this for...
                await base.OnDisconnectedAsync(exception);
                return;
            }

            // if user was in game.html page, 
            // and the groupId exist
            // and user is exist in the group
            // then remove the username from the group list
            if (!String.IsNullOrEmpty(groupId) 
                && groupList.ContainsKey(groupId)
                && groupList[groupId].UserScore.ContainsKey(username)
                ) 
            {
                Group group = groupList[groupId];            // Take the group of specific group id  
                string hostBefore = group.Host;              // Get the host before

                group.RemoveMember(username);                                              // Remove the member from the group
                await Groups.RemoveFromGroupAsync(cd[username].ConnectionId, groupId);     // Remove the connection from the group

                int memberCount = group.GetMemberCount();              // Count How many member in the current group
                bool isPublicAccess = group.IsPublicAccess;            // is the room accessible
                string host = group.Host;                              // get the hostname for later changing

                // if the grouplist is empty, remove the group entirely.
                if (memberCount <= 0) 
                {
                    groupList.TryRemove(groupId, out _);
                } 
                else {
                    // if everyone exit until 1 member left
                    // or there is only 1 member left
                    if (memberCount == 1) {

                        // if it was not in ready state before
                        if (group.Status != "Ready") {
                            // Reset The Game
                            groupList[groupId].ResetGame();
                            // Display Group Reset Game State
                            await Clients.Group(groupId).SendAsync("GroupResetGame");
                            // Display Ready Game State
                            await Clients.Group(groupId).SendAsync("ReadyGame");
                            // Clear All Interval
                            await Clients.Group(groupId).SendAsync("StateClearTimeout");
                            // Disable Canvas Tools for all people
                            await Clients.Group(groupId).SendAsync("DisableCanvas");
                            // Clear Canvas
                            await Clients.Group(groupId).SendAsync("ReceiveClear");
                        }
                        // show the start button, only for host
                        await Clients.Client(cd[host].ConnectionId).SendAsync("WaitingPlayerStatus");
                    }
                    else if (group.DrawerNow == username && group.DrawerDisconnected == false) {
                        // if the memberCount is not 1 only,
                        // and if user is drawer, 
                        // and havent disconnected before
                        // have to complete the setTimeout at the bottom
                        groupList[groupId].DrawerDisconnected = true;
                        isDrawerDisconnected = true;
                    }
                    else if (group.Status == "Drawing" && group.AllUserGuessedCorrect()) {
                        // if it was in drawing state, and all user has guessed
                        // then display Everyone Correct
                        await AnswerCorrect();
                    }
                    else if (group.Status == "Drawing" && group.ReportDrawerCountExceeded()) {
                        // if it was in drawing state, and accumulated reported user is matched
                        // report and skips the drawer
                        await DrawingReported();
                    }

                    // Refresh the user list
                    await RefreshUserList(groupId);
                    // display user quit message
                    await Clients.Group(groupId).SendAsync("UserQuitGame", username);

                    // if host has changed
                    if (hostBefore != group.Host) {
                        // Display Host Changed message
                        await Clients.Group(groupId).SendAsync("HostChangedGame", group.Host);
                    }
                }

                // Remove the room from room.html or reduce the member's number
                // If it is public accessible
                if (isPublicAccess) {
                    await Clients.Others.SendAsync("RemoveGameRoom", groupId, memberCount);
                }
            }
            
            // Remove the user from cd
            cd.TryRemove(username, out _);

            // changes
            // Private chat
            await Clients.All.SendAsync("RemoveUserChat", username);
            
            // No idea what is this for...
            await base.OnDisconnectedAsync(exception);

            // THIS IS FOR GAME DRAWER QUIT
            // VERY IMPORTANT
            if (isDrawerDisconnected) {

                // Get the group info
                Group group = groupList[groupId];

                // Get the progress time, if it was in certain state, it will be used
                DateTime currentDateTime = DateTime.Now;
                TimeSpan span = currentDateTime - group.ProgressStartTime;
                int ms = (int)span.TotalMilliseconds;

                // if drawer disconnected at DrawingReady and Drawing
                // Go to DrawingSkipped state
                if (group.Status == "DrawingReady" || group.Status == "Drawing") {
                    // Go to DrawingSkipped state
                    await DrawingSkipped();
                    ms = DrawingSkippedTime;
                }
                else if (group.Status == "DrawingLosesTurn") {
                    ms = DrawingLosesTurnTime - ms;
                }
                else if (group.Status == "DrawingSkipped") {
                    ms = DrawingSkippedTime - ms;
                }
                else if (group.Status == "DrawingReported") {
                    ms = DrawingReportedTime - ms;
                }
                else if (group.Status == "Answer") {
                    ms = AnswerTime - ms;
                }
                else if (group.Status == "AnswerCorrect") {
                    ms = AnswerCorrectTime - ms;
                }
                else if (group.Status == "Winner") {
                    ms = WinnerTime - ms;
                }

                // reset the drawing
                await Task.Delay(ms).ContinueWith(async (task) => {
                    groupList[groupId].DrawerDisconnected = false; 

                    // if the member count is not suddenly 1 left
                    // retain at ready state
                    // else go to drawingready again
                    if (group.GetMemberCount() > 1) {
                        await DrawingReady();
                    }
                });
            }
        }

        // HUB: room.html
        // Create a group
        // ===================================================================================================
        public async Task CreateGroup(bool isPublicAccess) 
        {
            string guid = Guid.NewGuid().ToString("N").Substring(0, 6);

            // Add to a group list
            if (groupList.TryAdd(guid, new Group(isPublicAccess, guid))) 
            {
                // then navigate to game.html
                await Clients.Caller.SendAsync("NavigateGroup", guid);
            } 
            else 
            {
                // if guid somehow duplicated, display error message and refresh
                await Clients.Caller.SendAsync("DisplayError", "Duplicate Error, Try Again Later");
            }
        }

        // HUB: room.html
        // Display All The Game Room
        // ===================================================================================================
        public async Task DisplayGameRoom() 
        {
            await Clients.Caller.SendAsync("ShowGameRoom", groupList);
        }

        
        // Send Simple Chat Message
        // ===================================================================================================
        public async Task SendSimpleMessage(string message) 
        {   
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            string username = Context.GetHttpContext().Request.Query["username"];

            // if empty string, do nothing
            if (message == "") {
                return;
            }

            Group group = groupList[groupId];
            string drawer = group.DrawerNow;

            // if it was in Drawing state
            // and the word is the same as answer, display warning message
            // =======
            // if it was in DrawingReady state, and user is drawer
            // and the word is the same as answer, display warning message
            if ((group.Status == "Drawing" && message.ToLower().Contains(group.WordsNow)) ||
                (group.Status == "DrawingReady" && username == drawer && message.ToLower().Contains(group.WordsNow))) {
                // Send warning to simple chat
                await Clients.Caller.SendAsync("ReceiveWarningMessage");
            }
            else {
                // Send to simple chat container
                await Clients.Group(groupId).SendAsync("ReceiveMessage", message, username, "#simpleChat");
            }
        }

        // Send Answer Chat Message
        // ===================================================================================================
        public async Task SendAnswerMessage(string message) 
        {   
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            string username = Context.GetHttpContext().Request.Query["username"];
            Group group = groupList[groupId];
            string word = group.WordsNow;
            string drawer = group.DrawerNow;

            // if it was not in drawing state
            // and message is empty
            // then do nothing
            if (group.Status != "Drawing" || message == "" || group.DrawerScoreHistory.ContainsKey(username)) {
                return;
            }

            // if user guess correct
            if (message.ToLower() == word) {

                // Add to score history
                group.AddScoreHistory(username);

                // sent the caller a correct message
                await Clients.Caller.SendAsync("ReceiveCorrectMessage", word);
                
                // tell other the caller has hit the answer
                await Clients.OthersInGroup(groupId).SendAsync("ReceiveHitMessage", username);

                // Refresh the user list
                await RefreshUserList(groupId);

                // if some user reached above ScoreMaximum, go to Winner State
                if (group.UserScore.Values.Max() >= ScoreMaximum) {
                    await Winner();
                }
                else if (group.AllUserGuessedCorrect()) {
                    // if all user has guessed correct, change to AnswerCorrect state
                    await AnswerCorrect();
                }
            }
            else {
                // Send to answer chat container
                await Clients.Group(groupId).SendAsync("ReceiveMessage", message, username, "#answerChat");
            }
        }

        public async Task ReportDrawer() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            string username = Context.GetHttpContext().Request.Query["username"];
            Group group = groupList[groupId];
            string drawer = group.DrawerNow;

            group.AddReportDrawerCount(username);
            int reportCount = group.GetReportDrawerCount();
            int memberCount = group.GetMemberCount();

            await Clients.Group(groupId).SendAsync("DrawerReportGame", drawer, username, reportCount, memberCount);

            if (group.ReportDrawerCountExceeded()) {
                await DrawingReported();
            }
        }   


        public async Task DrawingReady() 
        {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // if it was come from Winner state
            if (group.Status == "Winner") {
                // Restart the game
                group.ResetGame();
                await RefreshUserList(groupId);
                // Display Group Reset Game State
                await Clients.Group(groupId).SendAsync("GroupResetGame");
            }

            // Go to DrawingReady State
            group.Status = "DrawingReady";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            string drawer      = group.QueueDrawing();      // Get the drawer
            string word        = group.TakeWords();         // Get the random words
            string drawerConId = cd[drawer].ConnectionId;   // Get the drawer connection Id

            // Style the canvas to green
            await Clients.Client(drawerConId).SendAsync("CanvasGreen");
            // Clear Canvas
            group.CanvasURL = null;
            await Clients.Group(groupId).SendAsync("ReceiveClear");
            // Set Drawer Logo beside user
            await Clients.Group(groupId).SendAsync("SetDrawerLogo", drawer);
            // Display Drawing button to drawer
            await Clients.Client(drawerConId).SendAsync("TurnDrawingStatus", word, DrawingReadyTime, DrawingLosesTurnTime);
            // Display who's turn to guesser
            await Clients.GroupExcept(groupId, drawerConId).SendAsync("WhoTurnDrawingStatus", drawer, DrawingReadyTime);
            // Display DrawingReady Game State
            await Clients.Group(groupId).SendAsync("DrawingReadyGame", drawer);
        }

        public async Task DrawingLosesTurn() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // Go to DrawingLosesTurn State
            group.Status = "DrawingLosesTurn";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;
            string drawer = group.DrawerNow;
            string drawerConId = cd[drawer].ConnectionId;

            // Style the canvas back to red
            await Clients.Client(drawerConId).SendAsync("CanvasRed");
            // tell all the user the drawer loses its turn
            await Clients.Group(groupId).SendAsync("DrawingLosesTurnStatus", drawer, DrawingLosesTurnTime);
            // Display DrawingLosesTurn Game State
            await Clients.Group(groupId).SendAsync("DrawingLosesTurnGame", drawer);
        }

        public async Task Drawing() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // Go to Drawing State
            group.Status = "Drawing";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            string word = group.WordsNow;                 // Get the current word
            string drawer = group.DrawerNow;              // get the drawer
            string drawerConId = cd[drawer].ConnectionId; // Get the drawwer connection id

            await Clients.Client(drawerConId).SendAsync("EnableCanvas"); // To Enable Canvas for drawer
            await Clients.Client(drawerConId).SendAsync("DrawerDrawingStatus", word, DrawingTime, AnswerTime);
            await Clients.GroupExcept(groupId, drawerConId).SendAsync("WhoDrawingStatus", drawer, DrawingTime);
            // Display Drawing Game State
            await Clients.Group(groupId).SendAsync("DrawingGame", drawer);
        }

        public async Task DrawingSkipped() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // if it was from drawing state
            // refresh the group score
            await FromDrawingRefreshScore(groupId);

            // if it was come from drawing state
            // display interval in answer chat
            if (group.Status == "Drawing") {
                await Clients.Group(groupId).SendAsync("InsertInterval");
            }

            // Go to DrawingSkipped State
            group.Status = "DrawingSkipped";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;
            string drawer = group.DrawerNow;
            Customer drawerInfo;

            // invoke the drawing ready, if the drawer is still connecting
            if (cd.TryGetValue(drawer, out drawerInfo)) {
                string drawerConId = drawerInfo.ConnectionId;

                // let the drawer call the drawing ready timer again
                await Clients.Client(drawerConId).SendAsync("InvokeDrawingReady", DrawingSkippedTime);
                // Disable Canvas Tools for drawer
                await Clients.Client(drawerConId).SendAsync("DisableCanvas");
            }

            // let all the user know the drawer has skipped the turn
            await Clients.Group(groupId).SendAsync("DrawingSkippedStatus", drawer, DrawingSkippedTime);
            // Display DrawingSkipped Game State
            await Clients.Group(groupId).SendAsync("DrawingSkippedGame", drawer);
        }

        public async Task DrawingReported() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // if it was from drawing state
            // refresh the group score
            await FromDrawingRefreshScore(groupId);

            // Go to DrawingReported State
            group.Status = "DrawingReported";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            string drawer = group.DrawerNow;
            string drawerConId = cd[drawer].ConnectionId;

            // display interval in answer chat
            await Clients.Group(groupId).SendAsync("InsertInterval");
            // let the drawer call the drawing ready timer again
            await Clients.Client(drawerConId).SendAsync("InvokeDrawingReady", DrawingReportedTime);
            // let all the user know the drawer get reported
            await Clients.Group(groupId).SendAsync("DrawingReportedStatus", drawer, DrawingReportedTime);
            // Display DrawingReported Game State
            await Clients.Group(groupId).SendAsync("DrawingReportedGame", drawer);
            // Disable Canvas Tools for drawer
            await Clients.Client(drawerConId).SendAsync("DisableCanvas");
        }

        public async Task Answer() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // Go to Answer State
            group.Status = "Answer";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            // Reset the score per game
            group.ResetScore();

            string word = group.WordsNow;
            string drawer = group.DrawerNow;
            string drawerConId = cd[drawer].ConnectionId;

            // display interval in answer chat
            await Clients.Group(groupId).SendAsync("InsertInterval");
            // tell all the user the answer
            await Clients.Group(groupId).SendAsync("AnswerStatus", word, AnswerTime);
            // Display Answer Game State
            await Clients.Group(groupId).SendAsync("AnswerGame", word);
            // Disable Canvas Tools for drawer
            await Clients.Client(drawerConId).SendAsync("DisableCanvas");
        }

        public async Task AnswerCorrect() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // Go to AnswerCorrect State
            group.Status = "AnswerCorrect";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            // Reset the score per game
            group.ResetScore();
            
            string word = group.WordsNow;
            string drawer = group.DrawerNow;
            string drawerConId = cd[drawer].ConnectionId;

            // display interval in answer chat
            await Clients.Group(groupId).SendAsync("InsertInterval");
            // let the drawer call the drawing ready timer again
            await Clients.Client(drawerConId).SendAsync("InvokeDrawingReady", AnswerCorrectTime);
            // tell all the user everyone got the answer correct
            await Clients.Group(groupId).SendAsync("AnswerCorrectStatus", word, AnswerCorrectTime);
            // Display AnswerCorrect Game State
            await Clients.Group(groupId).SendAsync("AnswerCorrectGame", word);
            // Disable Canvas Tools for drawer
            await Clients.Client(drawerConId).SendAsync("DisableCanvas");
        }

        public async Task Winner() {
            // groupId querystring
            string groupId = Context.GetHttpContext().Request.Query["groupId"];
            Group group = groupList[groupId];

            // Go to Winner State
            group.Status = "Winner";

            // Start the progress bar, for drawer disconnection
            group.ProgressStartTime = DateTime.Now;

            // get the Top Three User
            var topThreeUser = group.UserScore.OrderByDescending(x => x.Value)
                                              .Select(x => x.Key)
                                              .Take(3)
                                              .ToList();

            string drawer = group.DrawerNow;
            string drawerConId = cd[drawer].ConnectionId;

            // Clear Canvas
            group.CanvasURL = null;
            await Clients.Group(groupId).SendAsync("ReceiveClear");
            // display interval in answer chat
            await Clients.Group(groupId).SendAsync("InsertInterval");
            // let the drawer call the drawing ready timer again
            await Clients.Client(drawerConId).SendAsync("InvokeDrawingReady", WinnerTime);
            // all user will state the winner status
            await Clients.Group(groupId).SendAsync("WinnerStatus", topThreeUser, WinnerTime);
            // Display Winner Game State
            await Clients.Group(groupId).SendAsync("WinnerGame", topThreeUser);
            // Disable Canvas Tools for drawer
            await Clients.Client(drawerConId).SendAsync("DisableCanvas");
        }

        // CANVAS FUNCTION ====================================================================================================
        public void RecordCanvas(string url) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            groupList[groupId].CanvasURL = url;
        }

        // Draw Fill Rectangle
        public async Task SendFillRect(Point lineStart, int lineWidth, Point rectSize, string strokeColor, string fillColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveFillRect", lineStart, lineWidth, rectSize, strokeColor, fillColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Stroke Rectangle
        public async Task SendStrokeRect(Point lineStart, int lineWidth, Point rectSize, string strokeColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveStrokeRect", lineStart, lineWidth, rectSize, strokeColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Fill Circle
        public async Task SendFillCircle(Point lineStart, int lineWidth, Point circleSize, Point circleMid, Point circleTranslate, string strokeColor, string fillColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveFillCircle", lineStart, lineWidth, circleSize, circleMid, circleTranslate, strokeColor, fillColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Stroke Circle
        public async Task SendStrokeCircle(Point lineStart, int lineWidth, Point circleSize, Point circleMid, Point circleTranslate, string strokeColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveStrokeCircle", lineStart, lineWidth, circleSize, circleMid, circleTranslate, strokeColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Line
        public async Task SendLine(Point a, Point b, int lineWidth, string strokeColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveLine", a, b, lineWidth, strokeColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Curve
        public async Task SendCurve(Point a, Point b, Point c, int lineWidth, string strokeColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveCurve", a, b, c, lineWidth, strokeColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Erase
        public async Task SendErase(int size, Point cursor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveErase", size, cursor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Draw Line Dash
        public async Task SendLineDash(Point a, Point b, int lineWidth, string strokeColor) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveLineDash", a, b, lineWidth, strokeColor);
            await Clients.Caller.SendAsync("GetCanvas");
        }

        // Clear Canvas
        public async Task SendClear() {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveClear");
            groupList[groupId].CanvasURL = null;
        }

        public async Task SendImageData(string data) {
            string groupId = Context.GetHttpContext().Request.Query["groupId"];

            await Clients.Group(groupId).SendAsync("ReceiveImageData", data);
            groupList[groupId].CanvasURL = data;
        }

        // =========================================================================================================

        // Private Chat ============================================================================================
        // changes
        // Display users in private chat

        // Send text in private chat
        public async Task SendMessage(string message, string selectedUserChat) 
        {
            // username querystring
            string username = Context.GetHttpContext().Request.Query["username"];

            await Clients.Caller.SendAsync("ReceiveChatMessage", username, message, "caller"); 
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("ReceiveChatMessage", username, message, "others");

            // Move Child To First
            await Clients.Caller.SendAsync("MoveChatToFirst", selectedUserChat);
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("MoveChatToFirst", username);

            // Play Notification for other user
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("Notification", username, "message");
        }

        // Send image in private chat
        public async Task SendImage(string url, string selectedUserChat)
        {
            // username querystring
            string username = Context.GetHttpContext().Request.Query["username"];

            await Clients.Caller.SendAsync("ReceiveChatImage", username, url, "caller");
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("ReceiveChatImage", username, url, "others");

            // Move Child To First
            await Clients.Caller.SendAsync("MoveChatToFirst", selectedUserChat);
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("MoveChatToFirst", username);

            // Play Notification for other user
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("Notification", username, "image");
        }

        // Send youtube video in private chat
        public async Task SendYouTube(string id, string selectedUserChat)
        {
            // username querystring
            string username = Context.GetHttpContext().Request.Query["username"];

            await Clients.Caller.SendAsync("ReceiveChatYouTube", username, id, "caller");
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("ReceiveChatYouTube", username, id, "others");

            // Move Child To First
            await Clients.Caller.SendAsync("MoveChatToFirst", selectedUserChat);
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("MoveChatToFirst", username);

            // Play Notification for other user
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("Notification", username, "youtube video");
        }

        // Store User History for others user
        public async Task StoreUserHistory(string message, string selectedUserChat) {
            // username querystring
            string username = Context.GetHttpContext().Request.Query["username"];

            await Clients.Caller.SendAsync("ReceiveUserHistory", selectedUserChat, message, "caller");
            await Clients.Client(cd[selectedUserChat].ConnectionId).SendAsync("ReceiveUserHistory", username, message, "others");
        }
        // until here

        
        // PRIVATE FUNCTION ===================================================================================================
        // Get remaining percentage progress bar
        private string GetPercentage(int ms, int statusTime) {
            return ((double)100 - ((double)ms / statusTime * 100)).ToString("0.##");;
        }

        // Refresh Client User List
        private async Task RefreshUserList(string groupId) {

            Group group = groupList[groupId];
            string drawer = "";
            
            // if it was in Drawing State
            // or DrawingReady State
            if (group.Status == "Drawing" || group.Status == "DrawingReady") {
                drawer = group.DrawerNow;
            }
            
            
            // Ordered the user based on score, for sorting userlist in client later
            var orderedUser = group.UserScore.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value); 
            // Refresh the client user list
            await Clients.Group(groupId).SendAsync("ShowUserList", orderedUser, group.Host, cd, drawer);
        }

        // if it was from drawing state
        // refresh the group score
        private async Task FromDrawingRefreshScore(string groupId) {

            Group group = groupList[groupId];

            // if it was come from drawing state
            if (group.Status == "Drawing") {
                // Redo the score
                group.RedoScore();
                // and then refresh the user list
                await RefreshUserList(groupId);
                // Reset the score
                group.ResetScore();
                // Display Score Reseted Game State
                await Clients.Group(groupId).SendAsync("ScoreResetGame");
            }
        }
    }
}