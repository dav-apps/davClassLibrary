namespace davClassLibrary
{
    public static class ErrorCodes
    {
        // Generic request errors
        public const int UnexpectedError = 1000;
        public const int AuthenticationFailed = 1001;
        public const int ActionNotAllowed = 1002;
        public const int ContentTypeNotSupported = 1003;

        // Errors for missing headers
        public const int AuthorizationHeaderMissing = 1100;
        public const int ContentTypeHeaderMissing = 1101;

        // File errors
        public const int ContentTypeDoesNotMatchFileType = 1200;
        public const int ImageFileInvalid = 1201;
        public const int ImageFileTooLarge = 1202;

        // Generic request body errors
        public const int InvalidBody = 2000;

        // Missing fields
        public const int AccessTokenMissing = 2100;
        public const int AppIdMissing = 2101;
        public const int TableIdMissing = 2102;
        public const int EmailMissing = 2103;
        public const int FirstNameMissing = 2104;
        public const int PasswordMissing = 2105;
        public const int EmailConfirmationTokenMissing = 2106;
        public const int PasswordConfirmationTokenMissing = 2107;
        public const int CountryMissing = 2108;
        public const int ApiKeyMissing = 2109;
        public const int NameMissing = 2110;
        public const int DescriptionMissing = 2111;
        public const int PropertiesMissing = 2112;
        public const int ProviderNameMissing = 2113;
        public const int ProviderImageMissing = 2114;
        public const int ProductNameMissing = 2115;
        public const int ProductImageMissing = 2116;
        public const int CurrencyMissing = 2117;
        public const int EndpointMissing = 2118;
        public const int P256dhMissing = 2119;
        public const int AuthMissing = 2120;
        public const int TimeMissing = 2121;
        public const int IntervalMissing = 2122;
        public const int TitleMissing = 2123;
        public const int BodyMissing = 2124;
        public const int PathMissing = 2125;
        public const int MethodMissing = 2126;
        public const int CommandsMissing = 2127;
        public const int ErrorsMissing = 2128;
        public const int EnvVarsMissing = 2129;

        // Fields with wrong type
        public const int AccessTokenWrongType = 2200;
        public const int UuidWrongType = 2201;
        public const int AppIdWrongType = 2202;
        public const int TableIdWrongType = 2203;
        public const int EmailWrongType = 2204;
        public const int FirstNameWrongType = 2205;
        public const int PasswordWrongType = 2206;
        public const int EmailConfirmationTokenWrongType = 2207;
        public const int PasswordConfirmationTokenWrongType = 2208;
        public const int CountryWrongType = 2209;
        public const int ApiKeyWrongType = 2210;
        public const int DeviceNameWrongType = 2211;
        public const int DeviceTypeWrongType = 2212;
        public const int DeviceOsWrongType = 2213;
        public const int NameWrongType = 2214;
        public const int DescriptionWrongType = 2215;
        public const int PublishedWrongType = 2216;
        public const int WebLinkWrongType = 2217;
        public const int GooglePlayLinkWrongType = 2218;
        public const int MicrosoftStoreLinkWrongType = 2219;
        public const int FileWrongType = 2220;
        public const int PropertiesWrongType = 2221;
        public const int PropertyNameWrongType = 2222;
        public const int PropertyValueWrongType = 2223;
        public const int ExtWrongType = 2224;
        public const int ProviderNameWrongType = 2225;
        public const int ProviderImageWrongType = 2226;
        public const int ProductNameWrongType = 2227;
        public const int ProductImageWrongType = 2228;
        public const int CurrencyWrongType = 2229;
        public const int EndpointWrongType = 2230;
        public const int P256dhWrongType = 2231;
        public const int AuthWrongType = 2232;
        public const int TimeWrongType = 2233;
        public const int IntervalWrongType = 2234;
        public const int TitleWrongType = 2235;
        public const int BodyWrongType = 2236;
        public const int PathWrongType = 2237;
        public const int MethodWrongType = 2238;
        public const int CommandsWrongType = 2239;
        public const int CachingWrongType = 2240;
        public const int ParamsWrongType = 2241;
        public const int ErrorsWrongType = 2242;
        public const int CodeWrongType = 2243;
        public const int MessageWrongType = 2244;
        public const int EnvVarsWrongType = 2245;
        public const int EnvVarNameWrongType = 2246;
        public const int EnvVarValueWrongType = 2247;

        // Too short fields
        public const int FirstNameTooShort = 2300;
        public const int PasswordTooShort = 2301;
        public const int DeviceNameTooShort = 2302;
        public const int DeviceTypeTooShort = 2303;
        public const int DeviceOsTooShort = 2304;
        public const int NameTooShort = 2305;
        public const int DescriptionTooShort = 2306;
        public const int WebLinkTooShort = 2307;
        public const int GooglePlayLinkTooShort = 2308;
        public const int MicrosoftStoreLinkTooShort = 2309;
        public const int PropertyNameTooShort = 2310;
        public const int PropertyValueTooShort = 2311;
        public const int ExtTooShort = 2312;
        public const int ProviderNameTooShort = 2313;
        public const int ProviderImageTooShort = 2314;
        public const int ProductNameTooShort = 2315;
        public const int ProductImageTooShort = 2316;
        public const int EndpointTooShort = 2317;
        public const int P256dhTooShort = 2318;
        public const int AuthTooShort = 2319;
        public const int TitleTooShort = 2320;
        public const int BodyTooShort = 2321;
        public const int PathTooShort = 2322;
        public const int CommandsTooShort = 2323;
        public const int ParamsTooShort = 2324;
        public const int MessageTooShort = 2325;
        public const int EnvVarNameTooShort = 2326;
        public const int EnvVarValueTooShort = 2327;

        // Too long fields
        public const int FirstNameTooLong = 2400;
        public const int PasswordTooLong = 2401;
        public const int DeviceNameTooLong = 2402;
        public const int DeviceTypeTooLong = 2403;
        public const int DeviceOsTooLong = 2404;
        public const int NameTooLong = 2405;
        public const int DescriptionTooLong = 2406;
        public const int WebLinkTooLong = 2407;
        public const int GooglePlayLinkTooLong = 2408;
        public const int MicrosoftStoreLinkTooLong = 2409;
        public const int PropertyNameTooLong = 2410;
        public const int PropertyValueTooLong = 2411;
        public const int ExtTooLong = 2412;
        public const int ProviderNameTooLong = 2413;
        public const int ProviderImageTooLong = 2414;
        public const int ProductNameTooLong = 2415;
        public const int ProductImageTooLong = 2416;
        public const int EndpointTooLong = 2417;
        public const int P256dhTooLong = 2418;
        public const int AuthTooLong = 2419;
        public const int TitleTooLong = 2420;
        public const int BodyTooLong = 2421;
        public const int PathTooLong = 2422;
        public const int CommandsTooLong = 2423;
        public const int ParamsTooLong = 2424;
        public const int MessageTooLong = 2425;
        public const int EnvVarNameTooLong = 2426;
        public const int EnvVarValueTooLong = 2427;

        // Invalid fields
        public const int EmailInvalid = 2500;
        public const int NameInvalid = 2501;
        public const int WebLinkInvalid = 2502;
        public const int GooglePlayLinkInvalid = 2503;
        public const int MicrosoftStoreLinkInvalid = 2504;
        public const int MethodInvalid = 2505;

        // Generic state errors
        public const int UserIsAlreadyConfirmed = 3000;
        public const int UserOfTableObjectMustHaveProvider = 3001;
        public const int UserAlreadyPurchasedThisTableObject = 3002;
        public const int UserHasNoPaymentInformation = 3003;
        public const int UserAlreadyHasStripeCustomer = 3004;
        public const int TableObjectIsNotFile = 3005;
        public const int TableObjectHasNoFile = 3006;
        public const int NotSufficientStorageAvailable = 3007;
        public const int PurchaseIsAlreadyCompleted = 3008;

        // Access token errors
        public const int CannotUseOldAccessToken = 3100;
        public const int AccessTokenMustBeRenewed = 3101;

        // Incorrect values
        public const int IncorrectPassword = 3200;
        public const int IncorrectEmailConfirmationToken = 3201;
        public const int IncorrectPasswordConfirmationToken = 3202;

        // Not supported values
        public const int CountryNotSupported = 3300;

        // Errors for values already in use
        public const int UuidAlreadyInUse = 3400;
        public const int EmailAlreadyInUse = 3401;

        // Errors for empty values in User
        public const int OldEmailOfUserIsEmpty = 3500;
        public const int NewEmailOfUserIsEmpty = 3501;
        public const int NewPasswordOfUserIsEmpty = 3502;

        // Errors for not existing resources
        public const int UserDoesNotExist = 3600;
        public const int DevDoesNotExist = 3601;
        public const int ProviderDoesNotExist = 3602;
        public const int SessionDoesNotExist = 3603;
        public const int AppDoesNotExist = 3604;
        public const int TableDoesNotExist = 3605;
        public const int TableObjectDoesNotExist = 3606;
        public const int TableObjectPriceDoesNotExist = 3607;
        public const int TableObjectUserAccessDoesNotExist = 3608;
        public const int PurchaseDoesNotExist = 3609;
        public const int WebPushSubscriptionDoesNotExist = 3610;
        public const int NotificationDoesNotExist = 3611;
        public const int ApiDoesNotExist = 3612;
        public const int ApiEndpointDoesNotExist = 3613;

        // Errors for already existing resources
        public const int UserAlreadyExists = 3700;
        public const int DevAlreadyExists = 3701;
        public const int ProviderAlreadyExists = 3702;
        public const int SessionAlreadyExists = 3703;
        public const int AppAlreadyExists = 3704;
        public const int TableAlreadyExists = 3705;
        public const int TableObjectAlreadyExists = 3706;
        public const int TableObjectPriceAlreadyExists = 3707;
        public const int TableObjectUserAccessAlreadyExists = 3708;
        public const int PurchaseAlreadyExists = 3709;
        public const int WebPushSubscriptionAlreadyExists = 3710;
        public const int NotificationAlreadyExists = 3711;
        public const int ApiAlreadyExists = 3712;
        public const int ApiEndpointAlreadyExists = 3713;
    }
}
