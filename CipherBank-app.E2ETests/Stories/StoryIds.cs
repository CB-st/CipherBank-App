namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// Stable story IDs shared with Expo Playwright (US-*) and the Draw.io scaffold (CB-*).
/// MAUI Appium owns design-spec E2E once Shell reaches Expo parity.
/// </summary>
public static class StoryIds
{
    // Account / onboarding
    public const string CbAccount001 = "CB-ACCOUNT-001";
    public const string CbAccount002 = "CB-ACCOUNT-002";
    public const string CbAccountPinChange = "CB-ACCOUNT-PIN-CHANGE";
    public const string UsOnb01 = "US-ONB-01";
    public const string UsOnb02 = "US-ONB-02";
    public const string UsOnb03 = "US-ONB-03";
    public const string UsOnb04 = "US-ONB-04";

    // Lock
    public const string UsLck01 = "US-LCK-01";
    public const string UsLck02 = "US-LCK-02";

    // Home / market
    public const string UsHom01 = "US-HOM-01";
    public const string UsHom05 = "US-HOM-05";
    public const string CbMarket001 = "CB-MARKET-001";

    // Convert / send / receive / pay
    public const string UsCnv01 = "US-CNV-01";
    public const string UsSnd01 = "US-SND-01";
    public const string UsRcv01 = "US-RCV-01";
    public const string UsPay01 = "US-PAY-01";
    public const string UsPos01 = "US-POS-01";
    public const string CbPay003 = "CB-PAY-003";

    // Wallets / fund / cards
    public const string CbWallet001 = "CB-WALLET-001";
    public const string CbWallet002 = "CB-WALLET-002";
    public const string CbFund001 = "CB-FUND-001";
    public const string CbFund002 = "CB-FUND-002";
    public const string CbCard001 = "CB-CARD-001";
    public const string CbCard002 = "CB-CARD-002";
    public const string CbPay001 = "CB-PAY-001";
    public const string CbPay002 = "CB-PAY-002";
}
