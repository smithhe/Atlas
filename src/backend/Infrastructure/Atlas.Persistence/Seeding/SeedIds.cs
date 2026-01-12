namespace Atlas.Persistence.Seeding;

/// <summary>
/// Deterministic IDs for dev/demo seed data.
///
/// Keep these stable: the frontend uses the same IDs to attach local-only extras (e.g. Azure work items, activity snapshots).
/// </summary>
public static class SeedIds
{
    // Settings
    public static readonly Guid Settings = Guid.Parse("a27602db-c255-42d4-987b-51f655cb10ad");

    // Team members
    public static readonly Guid TeamMemberAlice = Guid.Parse("7d14a395-70a2-4de7-867a-2e99e234f317");
    public static readonly Guid TeamMemberBob = Guid.Parse("0467c1b8-d98c-4772-b935-057429fdb452");
    public static readonly Guid TeamMemberCharlie = Guid.Parse("eb0ed93d-de7c-48de-a1d0-9b826102c525");
    public static readonly Guid TeamMemberDana = Guid.Parse("6accb600-4d25-439c-b6aa-d05797a2f4d2");
    public static readonly Guid TeamMemberEvan = Guid.Parse("5fbd9005-0b82-4aa6-ac23-b1f2c81c7646");

    // Product owners (separate entity from TeamMember)
    public static readonly Guid ProductOwnerDana = Guid.Parse("f2f943a8-356b-4bf6-b40d-63b7b6cafc71");
    public static readonly Guid ProductOwnerCharlie = Guid.Parse("465dc7bd-6099-4e1c-a65d-ee9ea395c22a");

    // Projects
    public static readonly Guid ProjectCorePlatform = Guid.Parse("0c17ca55-3429-4e16-bd47-dda755528c0e");
    public static readonly Guid ProjectDevEx = Guid.Parse("aec521c0-3e35-4c28-b746-2e112bd5ccd6");

    // Risks (global)
    public static readonly Guid RiskRefactorJustification = Guid.Parse("9eb36f85-733f-4b43-bde1-14633de76a1c");
    public static readonly Guid RiskOnboardingDrift = Guid.Parse("2a29c912-f4b9-4907-82e6-11f7334c9265");
    public static readonly Guid RiskOverCapacity = Guid.Parse("f5f58373-e45d-44f6-a91d-a58b24298c60");

    // Risk history entries
    public static readonly Guid RiskHistory1 = Guid.Parse("b54e1c5b-4049-4840-92d1-c0634f8f3ea1");
    public static readonly Guid RiskHistory2 = Guid.Parse("bb96ed0b-bfa5-4dc1-bf85-e3b50418f671");
    public static readonly Guid RiskHistory3 = Guid.Parse("1e4d01bd-053b-4a05-a924-abf4101cc76b");

    // Tasks
    public static readonly Guid Task1 = Guid.Parse("84414730-1339-430c-bb0c-5278214a74e0");
    public static readonly Guid Task2 = Guid.Parse("cab25af6-02b2-4ebd-8fc6-d11b3540c8b8");
    public static readonly Guid Task3 = Guid.Parse("15da5df4-612e-4b78-8da6-59f8ef667f6f");
    public static readonly Guid Task4 = Guid.Parse("23ba6108-13ed-427a-a30e-437808dc578d");
    public static readonly Guid Task5 = Guid.Parse("9f4685c0-a965-408d-8586-7d3cfa400ffd");
    public static readonly Guid Task6 = Guid.Parse("7cb3fa2c-d4e0-49b7-aea1-a68258090815");

    // Task dependencies (join entity has its own Guid PK)
    public static readonly Guid TaskDependency1 = Guid.Parse("7b8515d0-c6cc-48dd-8060-f966c14fd8e7"); // task-1 blocked by task-6

    // Team notes
    public static readonly Guid NoteAliceMarkdownDemo = Guid.Parse("b8a6d7dc-fde8-4cea-b04b-9a2d99114e50");
    public static readonly Guid NoteAlice7 = Guid.Parse("f830e22e-d02c-4fc7-a691-2d043bc880e8");
    public static readonly Guid NoteAlice6 = Guid.Parse("b87ec1be-597f-43f1-aa99-d2f03c3cb647");
    public static readonly Guid NoteAlice5 = Guid.Parse("413c3768-a478-4aac-b3f4-ec3b521735d6");
    public static readonly Guid NoteAlice4 = Guid.Parse("e55f4d46-d183-42a5-9681-e5bd7921351f");
    public static readonly Guid NoteAlice3 = Guid.Parse("5b06d958-d37c-4a07-9942-9c8532a46f82");
    public static readonly Guid NoteAlice2 = Guid.Parse("926a6954-499e-495b-a9ef-3d4726acafa1");
    public static readonly Guid NoteAlice1 = Guid.Parse("f1938a2d-b93c-41fd-93d3-5bf8b921556e");

    public static readonly Guid NoteBob1 = Guid.Parse("d68f3b03-c123-47a5-a4dd-ff1a11a70b50");
    public static readonly Guid NoteDana1 = Guid.Parse("63d8667a-5bfa-411b-ac1c-1e0fe22b9a9d");
    public static readonly Guid NoteEvan1 = Guid.Parse("99ed6327-1327-421a-bb9f-5184f9fa1d41");

    // Team member risks
    public static readonly Guid TeamMemberRiskBob = Guid.Parse("7cc5443b-bd0d-4484-a801-cc7356de7ba0");
    public static readonly Guid TeamMemberRiskAlice = Guid.Parse("bf476263-36f4-40ac-bb79-e4f49e39d232");
    public static readonly Guid TeamMemberRiskEvan = Guid.Parse("ef2daa9e-41fd-4868-812a-150316f26e59");

    // Growth (Alice)
    public static readonly Guid GrowthAlice = Guid.Parse("f885064e-4158-426e-b893-c2a334dc33c1");

    public static readonly Guid GrowthGoalAlice1 = Guid.Parse("1576fbe4-dc3c-43f0-8800-919f70a8aa80");
    public static readonly Guid GrowthGoalAlice2 = Guid.Parse("305ad6bf-ec84-47b9-a88f-baa12d7875fb");

    public static readonly Guid GrowthThemeAlice1 = Guid.Parse("1d55eea0-b8a0-49b8-8612-11afa6080178");
    public static readonly Guid GrowthThemeAlice2 = Guid.Parse("d47f5111-3ba9-4b28-bbd5-671d0d983dc2");

    // Growth actions (Alice)
    public static readonly Guid GrowthActionAlice1 = Guid.Parse("21b06de1-c086-4326-b59c-76b5436a81ef");
    public static readonly Guid GrowthActionAlice2 = Guid.Parse("00fbc385-bcba-4dd3-abe7-39a60fb9ba2e");
    public static readonly Guid GrowthActionAlice3 = Guid.Parse("4510e522-5f66-4e0b-b70b-4f02d450b794");
    public static readonly Guid GrowthActionAlice4 = Guid.Parse("5387c800-1532-4f12-8349-ded157eeb375");
    public static readonly Guid GrowthActionAlice5 = Guid.Parse("8ac555aa-d603-4773-8979-2d6205fabba6");
    public static readonly Guid GrowthActionAlice6 = Guid.Parse("a257c2d0-deed-400d-99cb-fcbc071e2688");
    public static readonly Guid GrowthActionAlice7 = Guid.Parse("2cafa262-cd58-4f69-9fd2-51f0e86850bb");
    public static readonly Guid GrowthActionAlice8 = Guid.Parse("01fbdf18-33c8-4b89-be5b-25c7756597f2");
    public static readonly Guid GrowthActionAlice9 = Guid.Parse("cccbd6b4-636e-4e90-acda-bd524289e99e");

    // Growth check-ins (Alice)
    public static readonly Guid GrowthCheckInAlice1 = Guid.Parse("e5dc89e0-fd9f-40d3-a501-a13864aa2f70");
    public static readonly Guid GrowthCheckInAlice2 = Guid.Parse("0a49a2ed-442e-4aff-87c6-ad8096559fde");
    public static readonly Guid GrowthCheckInAlice3 = Guid.Parse("f2560a84-619b-4a91-bd9c-08dff47b9a38");
    public static readonly Guid GrowthCheckInAlice4 = Guid.Parse("9b56e8ed-bb65-409f-8ea2-6051fd040e39");
    public static readonly Guid GrowthCheckInAlice5 = Guid.Parse("d2f463d9-7cd7-4c20-83c2-99ae7caaa3a2");
    public static readonly Guid GrowthCheckInAlice6 = Guid.Parse("9057251d-0612-47e4-ad6c-3b5d50d24ffb");
    public static readonly Guid GrowthCheckInAlice7 = Guid.Parse("7ff3eb4e-b157-48e8-8421-7afcbb85f8a5");
}

