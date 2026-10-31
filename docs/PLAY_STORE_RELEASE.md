# KidoWorld Coloring — Google Play release checklist

Status: production foundations implemented; release intentionally blocked until the items marked **Owner action** are complete.

## Locked decisions

- Developer/company: **KidoWorld**
- Android package: **`com.kidoworld.coloring`**
- Product model: nine free canvases plus one lifetime unlock for all 30
- Google Play product ID: **`kidoworld_full_access`**
- Version-one advertising: **none**
- Target audience: children ages 3–6; Google Play Families policies apply
- Android target: API 36+, ARM64, IL2CPP, Android App Bundle

The package ID is permanent after the Play Console app is created. Confirm that `com.kidoworld.coloring` is accepted before distributing builds outside the test team.

## Implemented in the Unity project

- Nine free canvases, three from each world; remaining canvases use the existing grown-up gate.
- A store-neutral purchase/restore boundary. Access is granted only after a provider returns Purchased or Restored.
- Development purchase simulation compiles only in the Editor or Development builds and uses a separate entitlement key.
- No advertising path or ad SDK.
- Privacy links remain behind the grown-up gate.
- Permanent package identity and non-secret Android build settings.
- `Kido Coloring/6. Validate Play Store release` reports every unresolved release blocker.
- `Kido Coloring/7. Build signed Android App Bundle` refuses to build until validation passes.

## Owner action — required before a production bundle

1. Create the app in Play Console with package `com.kidoworld.coloring`.
2. Create a managed one-time product named `kidoworld_full_access`; choose localized US and UK pricing.
3. Approve adding Unity IAP (or another Google Play Billing adapter) to `Packages/`, then implement and test purchase, pending, cancellation, acknowledgement, ownership refresh, restore and refund/revocation behavior.
4. Change `billingProvider` in `settings.json` to `unity_iap` only after the adapter passes store-track tests.
5. Complete and legally review `docs/PRIVACY_POLICY_DRAFT.md`; publish it at a stable HTTPS URL and set `privacyPolicyUrl`.
6. Create a private upload keystore locally. Never commit the keystore, alias password, store credentials or service-account JSON.
7. Run the native EditMode suite and profile phone/tablet builds on supported Android devices.
8. Complete Play Console Data safety, Target audience and content, Families, content rating and app-access declarations accurately.
9. Upload the signed `.aab` to Internal testing, then Closed testing.
10. If the developer account is a new personal account, maintain the currently required closed-test participation before requesting production access.

## Store listing assets

- 512 × 512 Play Store icon
- 1024 × 500 feature graphic
- Phone and tablet screenshots showing actual gameplay
- Short and full descriptions without unsupported claims
- Support email and privacy-policy URL
- Optional gameplay video without ads or misleading UI

## Local release workflow

1. Pull the approved release branch.
2. Open with Unity 6000.3.14f1.
3. Run `Kido Coloring/5. Apply non-secret Play Store settings`.
4. Configure the upload keystore locally in Player Settings.
5. Run `Kido Coloring/6. Validate Play Store release`.
6. Resolve every reported blocker.
7. Run `Kido Coloring/7. Build signed Android App Bundle`.

Development APKs remain available for device QA and are not production artifacts.
