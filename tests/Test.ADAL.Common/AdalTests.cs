﻿//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal enum TestType
    {
        DotNet,
        WinRT
    }

    internal partial class AdalTests
    {
        private const string SecondCallExtraQueryParameter = "secondcall";
        private const string ThirdCallExtraQueryParameter = "thirdcall";
        public static TestType TestType { get; set; }
        public static IPlatformParameters PlatformParameters { get; set; }

        public static void InitializeTest()
        {
            AuthenticationContextProxy.InitializeTest();
        }

        public static async Task AcquireTokenPositiveTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetEnvironmentVariable("ExtraQueryParameter", "slice=testslice&nux=1&msaproxy=true");
            SetCredential(sts);

            var context = new AuthenticationContextProxy("https://login.microsoftonline.com/common/", sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(new[] { "https://outlook.office.com/Mail.Read" }, null,
                sts.ValidClientId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                        PlatformParameters, UserIdentifier.AnyUser, "slice=testslice&nux=1&msaproxy=true");
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy idTokenResult =
                await
                    context.AcquireTokenSilentAsync(new[] {"e1eb8a8d-7b0c-4a14-9313-3f2c25c82929"},
                        "e1eb8a8d-7b0c-4a14-9313-3f2c25c82929", UserIdentifier.AnyUser);
            Verify.AreNotEqual(result.Token, idTokenResult.Token);
        }

        public static async Task AcquireTokenPositiveWithoutRedirectUriOrUserIdTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "userId");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "UserIdentifier.AnyUser");

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static async Task AuthenticationContextAuthorityValidationTestAsync(Sts sts)
        {
            SetCredential(sts);
            AuthenticationContextProxy context = null;
            AuthenticationResultProxy result = null;
            try
            {
                context = new AuthenticationContextProxy(sts.InvalidAuthority, true);
                //Verify.AreNotEqual(sts.Type, StsType.ADFS);
                result =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifyErrorResult(result, Sts.AuthorityNotInValidList, "authority");
            }
            catch (ArgumentException ex)
            {
               // Verify.AreEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ParamName, "validateAuthority");
            }
#if TEST_ADAL_WINPHONE_UNIT
            catch (AdalServiceException ex)
            {
                //Verify.AreNotEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ErrorCode, Sts.AuthorityNotInValidList);
                Verify.IsTrue(ex.Message.Contains("authority"));
            }
#endif

            context = new AuthenticationContextProxy(sts.InvalidAuthority, false);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, "authentication dialog");
            context = new AuthenticationContextProxy(sts.Authority, false);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

                context = new AuthenticationContextProxy(sts.Authority, true);
                result =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifySuccessResult(sts, result);

            try
            {
                context = new AuthenticationContextProxy(sts.InvalidAuthority);
                //Verify.AreNotEqual(sts.Type, StsType.ADFS);
                result =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifyErrorResult(result, Sts.AuthorityNotInValidList, "authority");
            }
            catch (ArgumentException ex)
            {
               // Verify.AreEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ParamName, "validateAuthority");
            }
#if TEST_ADAL_WINPHONE_UNIT
            catch (AdalServiceException ex)
            {
               // Verify.AreNotEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ErrorCode, Sts.AuthorityNotInValidList);
                Verify.IsTrue(ex.Message.Contains("authority"));
            }
#endif

            context = new AuthenticationContextProxy(sts.Authority + "/extraPath1/extraPath2", sts.ValidateAuthority);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }

        public static async Task AcquireTokenWithRedirectUriTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.InvalidExistingRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.InvalidNonExistingRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId,
                        new Uri(sts.ValidNonExistingRedirectUri.OriginalString + "#fragment"), PlatformParameters,
                        sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "fragment");

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, null, PlatformParameters,
                        sts.ValidUserId);
            if (TestType != TestType.WinRT)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            }
            else
            {
                // Winrt can send null redirecturi
                VerifySuccessResult(sts, result);
            }

            AuthenticationContextProxy.ClearDefaultCache();
            EndBrowserDialogSession();
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientIdWithExistingRedirectUri,
                        sts.ValidExistingRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.ClearDefaultCache();

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidNonExistentRedirectUriClientId,
                        sts.ValidNonExistingRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }

        public static async Task AcquireTokenWithInvalidAuthorityTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy("https://www.outlook.com/login", false);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, null);

            context = new AuthenticationContextProxy(sts.InvalidAuthority, false);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, null);

                Uri uri = new Uri(sts.Authority);
                context =
                    new AuthenticationContextProxy(string.Format("{0}://{1}/non_existing_tenant", uri.Scheme,
                        uri.Authority));
                result =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
        }

        public static async Task AcquireTokenWithInvalidResourceTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.InvalidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidResourceError, "resource");

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(new[] {Guid.NewGuid().ToString()}, null, sts.ValidClientId.ToUpper(),
                        sts.ValidDefaultRedirectUri, PlatformParameters,
                        (sts.Type == StsType.AAD)
                            ? new UserIdentifier(sts.ValidUserName, UserIdentifierType.RequiredDisplayableId)
                            : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(new[] {Guid.NewGuid().ToString()}, null, sts.ValidClientId.ToUpper(),
                        sts.ValidDefaultRedirectUri, PlatformParameters,
                        (result.UserInfo != null)
                            ? new UserIdentifier(result.UserInfo.UniqueId, UserIdentifierType.UniqueId)
                            : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static async Task AcquireTokenWithInvalidClientIdTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.InvalidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
        }

        public static async Task AcquireTokenWithIncorrectUserCredentialTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.InvalidUserName, "invalid_password");
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, UserIdentifier.AnyUser, "incorrect_user");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }

        public static async Task AcquireTokenWithAuthenticationCanceledTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(null, null);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters,
                        new UserIdentifier("cancel_authentication@test.com", UserIdentifierType.OptionalDisplayableId));
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }

        public static async Task AcquireTokenPositiveWithDefaultCacheTestAsync(Sts sts)
        {
            AuthenticationContextProxy.ClearDefaultCache();

            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);

            EndBrowserDialogSession();
            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000); // 2 seconds delay
            AuthenticationResultProxy resultWithoutUser =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, UserIdentifier.AnyUser, SecondCallExtraQueryParameter);
            VerifyExpiresOnAreEqual(results[0], resultWithoutUser);

            context.VerifySingleItemInCache(results[0], sts.Type);
        }

        public static async Task AcquireTokenPositiveWithNullCacheTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var context = new AuthenticationContextProxy(
                sts.Authority,
                sts.ValidateAuthority,
                TokenCacheType.Null);
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);
            VerifyExpiresOnAreNotEqual(results[0], results[1]);
        }

        public static async Task AcquireTokenPositiveWithInMemoryCacheTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.InMemory);
            List<AuthenticationResultProxy> results =
                await AcquireTokenPositiveWithCacheExpectingEqualResultsAsync(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);
        }

        public static async Task UserInfoTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2;
            if (sts.Type == StsType.AAD)
            {
                Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);
                Verify.IsNotNullOrEmptyString(result.UserInfo.UniqueId);
                Verify.IsNotNullOrEmptyString(result.UserInfo.GivenName);
                Verify.IsNotNullOrEmptyString(result.UserInfo.FamilyName);

                EndBrowserDialogSession();
                Log.Comment("Waiting 2 seconds before next token request...");
                AuthenticationContextProxy.Delay(2000); // 2 seconds delay
                AuthenticationContextProxy.SetCredentials(null, null);
                result2 =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters,
                            new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId),
                            SecondCallExtraQueryParameter);
                ValidateAuthenticationResultsAreEqual(result, result2);
            }

            AuthenticationContextProxy.SetCredentials(null, null);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters);
            Verify.AreEqual(result.Token, result2.Token);

            SetCredential(sts);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, ThirdCallExtraQueryParameter);
            VerifySuccessResult(sts, result2);
            if (result.UserInfo != null)
            {
                ValidateAuthenticationResultsAreEqual(result, result2);
            }
            else
            {
                VerifyExpiresOnAreNotEqual(result, result2);
            }

            EndBrowserDialogSession();
            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000); // 2 seconds delay
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.InvalidRequiredUserId, SecondCallExtraQueryParameter);
            VerifyErrorResult(result2, "user_mismatch", null);
        }

        public static async Task MultiResourceRefreshTokenTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope2, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result2);

            if (sts.Type == StsType.AAD)
            {
                result2 =
                    await
                        context.AcquireTokenAsync(sts.ValidScope3, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifySuccessResult(sts, result2);
            }
        }

        public static async Task TenantlessTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.TenantlessAuthority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            Verify.IsNotNullOrEmptyString(result.TenantId);

            AuthenticationContextProxy.SetCredentials(null, null);
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(
                sts.ValidScope,
                null,
                sts.ValidClientId,
                sts.ValidDefaultRedirectUri,
                PlatformParameters,
                sts.ValidUserId);

            ValidateAuthenticationResultsAreEqual(result, result2);

            SetCredential(sts);
            context = new AuthenticationContextProxy(sts.TenantlessAuthority.Replace("Common", result.TenantId),
                sts.ValidateAuthority, TokenCacheType.Null);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
        }

        public static async Task InstanceDiscoveryTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetEnvironmentVariable("ExtraQueryParameter", string.Empty);
            try
            {
                context = new AuthenticationContextProxy(sts.Authority.Replace("windows.net", "windows.unknown"),
                    sts.ValidateAuthority);
                result =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters, sts.ValidUserId);
                VerifyErrorResult(result, "authority_not_in_valid_list", "authority");
            }
#if TEST_ADAL_WINPHONE_UNIT
            catch (AdalServiceException ex)
            {
                //Verify.AreNotEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ErrorCode, Sts.AuthorityNotInValidList);
                Verify.IsTrue(ex.Message.Contains("authority"));
            }
#endif
            finally
            {
            }
        }

        public static async Task AcquireTokenPositiveWithFederatedTenantTestAsync(Sts sts)
        {
            var userId = sts.ValidUserId;

            AuthenticationContextProxy.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, false, TokenCacheType.Null);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, userId);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static async Task InnerExceptionAccessTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            result = await context.AcquireTokenSilentAsync(sts.ValidScope, sts.InvalidClientId);
            VerifyErrorResult(result, "failed_to_acquire_token_silently", null);
            Verify.IsNotNull(result.Exception);
        }

        public static async Task ExtraQueryParametersTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.Null);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, null);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, "redirect_uri=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "redirect_uri");

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, "resource=123&dummy=dummy_value#$%^@%^^%");
            VerifyErrorResult(result, "duplicate_query_parameter", "resource");

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, "client_id=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "client_id");

            EndBrowserDialogSession();
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, "login_hint=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "login_hint");

            EndBrowserDialogSession();
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, "login_hintx=123");
            VerifySuccessResult(sts, result);

            EndBrowserDialogSession();
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, UserIdentifier.AnyUser, "login_hint=" + sts.ValidUserName);
            VerifySuccessResult(sts, result);

            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId, string.Empty);
            VerifySuccessResult(sts, result);
        }

        internal static async Task MultiUserCacheTestAsync(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Clear cookie and acquire token for user2 interactively");
            EndBrowserDialogSession();
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword2);
            AuthenticationResultProxy result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result =
                await
                    context.AcquireTokenAsync(sts.ValidScope2, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 =
                await
                    context.AcquireTokenAsync(sts.ValidScope2, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);
        }

        public static void VerifyExpiresOnAreEqual(AuthenticationResultProxy result, AuthenticationResultProxy result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (!equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R") + " <> " + result2.ExpiresOn.ToString("R"));
            }

            Verify.IsTrue(equal, "AuthenticationResult.ExpiresOn");
        }

        public static void VerifyExpiresOnAreNotEqual(AuthenticationResultProxy result,
            AuthenticationResultProxy result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R") + " <> " + result2.ExpiresOn.ToString("R"));
            }

            Verify.IsFalse(equal, "AuthenticationResult.ExpiresOn");
        }

        public static bool AreDateTimeOffsetsEqual(DateTimeOffset time1, DateTimeOffset time2)
        {
            bool equal = (time1.Ticks/10000 == time2.Ticks/10000);
            if (!equal)
            {
                Log.Comment("DateTimeOffsets with ticks {0} and {1} are not equal", time1.Ticks, time2.Ticks);
            }

            return equal;
        }

        public static async Task<List<AuthenticationResultProxy>> AcquireTokenPositiveWithCacheAsync(Sts sts,
            AuthenticationContextProxy context)
        {
            AuthenticationResultProxy result =
                await
                    context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                        PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000); // 2 seconds delay

            AuthenticationResultProxy result2;
            if (result.UserInfo != null)
                result2 =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters,
                            new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId),
                            SecondCallExtraQueryParameter);
            else
                result2 =
                    await
                        context.AcquireTokenAsync(sts.ValidScope, null, sts.ValidClientId, sts.ValidDefaultRedirectUri,
                            PlatformParameters);

            VerifySuccessResult(sts, result2);

            return new List<AuthenticationResultProxy> {result, result2};
        }

        public static void EndBrowserDialogSession()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            NativeMethods.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        public static void VerifySuccessResult(Sts sts, AuthenticationResultProxy result,
            bool supportRefreshToken = true, bool supportUserInfo = true)
        {
            Log.Comment("Verifying success result...");
            if (result.Status != AuthenticationStatusProxy.Success)
            {
                Log.Comment(string.Format("Unexpected '{0}' error from service: {1}", result.Error,
                    result.ErrorDescription));
            }

            Verify.AreEqual(AuthenticationStatusProxy.Success, result.Status, "AuthenticationResult.Status");
            Verify.IsNotNullOrEmptyString(result.Token, "AuthenticationResult.Token");

            Verify.IsNullOrEmptyString(result.Error, "AuthenticationResult.Error");
            Verify.IsNullOrEmptyString(result.ErrorDescription, "AuthenticationResult.ErrorDescription");

            if (/*sts.Type != StsType.ADFS && */supportUserInfo)
            {
                Action<string, string, bool> ValidateUserInfo = (string field, string caption, bool required) =>
                {
                    if (string.IsNullOrEmpty(field))
                    {
                        if (required)
                        {
                            Log.Error("No " + caption);
                        }
                        else
                        {
                            Log.Warning("No " + caption);
                        }
                    }
                    else
                    {
                        Log.Comment(field, caption);
                    }
                };

                ValidateUserInfo(result.TenantId, "tenant id", true);
                ValidateUserInfo(result.UserInfo.UniqueId, "user unique id", true);
                ValidateUserInfo(result.UserInfo.DisplayableId, "user displayable id", true);
                //ValidateUserInfo(result.UserInfo.IdentityProvider, "identity provider", true);
                ValidateUserInfo(result.UserInfo.GivenName, "given name", false);
                ValidateUserInfo(result.UserInfo.FamilyName, "family name", false);
            }

            long expiresIn = (long) (result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long) 0, "accessToken ExpiresOn");
        }

        public static void VerifyErrorResult(AuthenticationResultProxy result, string error,
            string errorDescriptionKeyword, int statusCode = 0, string serviceErrorCode = null)
        {
            Log.Comment(string.Format("Verifying error result '{0}':'{1}'...", result.Error, result.ErrorDescription));
            Verify.AreNotEqual(AuthenticationStatusProxy.Success, result.Status);
            Verify.IsNullOrEmptyString(result.Token);
            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.IsNotNullOrEmptyString(result.ErrorDescription);
            Verify.IsFalse(result.ErrorDescription.Contains("+"),
                "Error description should not be in URL form encoding!");
            Verify.IsFalse(result.ErrorDescription.Contains("%2"), "Error description should not be in URL encoding!");

            if (!string.IsNullOrEmpty(error))
            {
                Verify.AreEqual(error, result.Error);
            }

            if (!string.IsNullOrEmpty(errorDescriptionKeyword))
            {
                VerifyErrorDescriptionContains(result.ErrorDescription, errorDescriptionKeyword);
            }

            if (statusCode != 0)
            {
                Verify.AreEqual(statusCode, result.ExceptionStatusCode);
            }

            if (serviceErrorCode != null)
            {
                Verify.IsTrue(result.ExceptionServiceErrorCodes.Contains(serviceErrorCode));
            }
        }

        private static async Task<List<AuthenticationResultProxy>>
            AcquireTokenPositiveWithCacheExpectingEqualResultsAsync(Sts sts, AuthenticationContextProxy context)
        {
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);

            Verify.AreEqual(results[0].Token, results[1].Token, "AuthenticationResult.Token");
            Log.Comment(string.Format("First ExpiresOn: {0}", results[0].ExpiresOn));
            Log.Comment(string.Format("Second ExpiresOn: {0}", results[1].ExpiresOn));
            return results;
        }

        private static void VerifyErrorDescriptionContains(string errorDescription, string keyword)
        {
            Log.Comment(string.Format("Verifying error description '{0}'...", errorDescription));
            Verify.IsGreaterThanOrEqual(errorDescription.IndexOf(keyword, StringComparison.OrdinalIgnoreCase), 0);
        }

        private static void ValidateAuthenticationResultsAreEqual(AuthenticationResultProxy result,
            AuthenticationResultProxy result2)
        {
            Verify.AreEqual(result.Token, result2.Token, "AuthenticationResult.Token");
            Verify.AreEqual(result.UserInfo.UniqueId, result2.UserInfo.UniqueId);
            Verify.AreEqual(result.UserInfo.DisplayableId, result2.UserInfo.DisplayableId);
            Verify.AreEqual(result.UserInfo.GivenName, result2.UserInfo.GivenName);
            Verify.AreEqual(result.UserInfo.FamilyName, result2.UserInfo.FamilyName);
            Verify.AreEqual(result.TenantId, result2.TenantId);
        }

        private static void SetCredential(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(/*sts.Type == StsType.ADFS ? sts.ValidUserName : null*/sts.ValidUserName,
                sts.ValidPassword);
        }

        private static class NativeMethods
        {
            [DllImport("wininet.dll", SetLastError = true)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer,
                int lpdwBufferLength);
        }
    }
}