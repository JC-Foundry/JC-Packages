// using System.Collections.Concurrent;
//
// namespace JC.Identity.Shared.Authentication;
//
// public class AuthenticationRefresh
// {
//     private readonly ConcurrentDictionary<string, byte> _refreshSignIns = new();
//
//     public void RefreshUserSignIn(string userId)
//     {
//         _refreshSignIns[userId] = 0;
//     }
//     
//     public bool IsRefreshPending(string userId)
//     {
//         return _refreshSignIns.ContainsKey(userId);
//     }
//
//     public void ConsumeRefreshSignIn(string userId)
//     {
//         _refreshSignIns.TryRemove(userId, out _);
//     }
// }