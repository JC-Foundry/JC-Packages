// using JC.Core.Models;
// using JC.Identity.Shared.Authentication;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Identity;
//
// namespace JC.Identity.Shared.Web.Middleware;
//
// public class AuthenticationRefreshMiddleware(RequestDelegate next)
// {
//     public async Task InvokeAsync<TSignInManager, T>(HttpContext context,
//         AuthenticationRefresh authRefresh,
//         TSignInManager signInManager,
//         IUserInfo userInfo)
//         where TSignInManager : SignInManager<T>
//         where T : class
//     {
//         var userId = userInfo.UserId;
//         if (!string.IsNullOrEmpty(userId) && authRefresh.IsRefreshPending(userId))
//         {
//             // GetUserAsync resolves the AppUser from the current principal — we only ever refresh the
//             // requester's own session, never the target's.
//             var user = await signInManager.UserManager.GetUserAsync(context.User);
//             if (user != null)
//                 await signInManager.RefreshSignInAsync(user);
//
//             authRefresh.ConsumeRefreshSignIn(userId);
//         }
//
//         await next(context);
//     }
// }