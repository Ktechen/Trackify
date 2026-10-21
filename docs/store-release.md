# Store release: what is ready, and what only you can do

The Uno app (`Source/Trackify`) on the three stores: Google Play (Android), the Apple App Store
(iOS) and the Microsoft Store (Windows, MSIX). The Pi CLI is not a store product. It ships as a
Docker image or `.deb`, see `Source/Trackify.Cli/ReadMe.md`.

Status: **2026-10-21**. Store rules change every year, so check the dates in the
[requirements table](#store-requirements-checked-on-2026-10-21) before you upload.

**Order, decided 2026-10-21:** **Google Play first.** Start the closed test now, because a new
personal Play account has to run 12 testers for 14 days before production unlocks, and that clock
is the longest lead time of all. Run the **Microsoft Store in parallel**: registration is free and
the Windows head builds here. The **App Store comes last**, because it needs a Mac, Xcode 26 and
the paid developer programme.

### Next steps for Google Play + Microsoft Store

1. Play Console account → upload key → privacy policy online ([Google Play](#google-play-android),
   steps 1–3).
2. The release pipeline produces a signed `.aab` from a `vX.Y.Z` tag
   ([what CI needs](#what-ci-needs-for-the-release-pipeline-task-not-built-here)) → closed-testing
   track → recruit 12 testers. The 14 days start once they have opted in.
3. In parallel: Partner Center account → reserve the name → copy the identity into
   `Package.appxmanifest` ([Microsoft Store](#microsoft-store-windows), steps 1–2). The release
   pipeline then builds the Store `.msixupload`.
4. Fill in the store listings from the [drafts](#store-listing-drafts) and capture the
   screenshots.

---

## At a glance

| | Google Play | App Store | Microsoft Store |
|---|---|---|---|
| App identity | ✅ `com.ktechen.trackify` | ✅ same bundle id | ⚠️ Name/Publisher must come from Partner Center |
| Version | ⏳ from the `vX.Y.Z` tag (release pipeline) | ⏳ same | ⏳ same, with revision 0 (release pipeline) |
| Release build | ✅ fixed: AAB restore error on SDK 10.0.2xx | ❓ needs a Mac | ✅ builds here |
| Store package | ⏳ `.aab` from the release pipeline | ❓ `.ipa` on a Mac | ⏳ `.msixupload` from the release pipeline |
| Permissions / usage texts | ✅ BLE correct; INTERNET added | ✅ Bluetooth + local network texts | ✅ Bluetooth + network capabilities added |
| Privacy | ✅ nothing collected; Data safety answers below | ✅ privacy manifest repaired | ✅ |
| Export compliance | n/a | ✅ `ITSAppUsesNonExemptEncryption = false` | n/a |
| Signing | 🔑 you: upload key | 🔑 you: certificates via Xcode | 🔑 Store signs, sideload needs your cert |
| Privacy policy URL | 🔑 you: host the [draft](#privacy-policy-draft) | 🔑 you | 🔑 you |
| Store texts | 📝 [drafts](#store-listing-drafts) to review | 📝 drafts | 📝 drafts |
| Screenshots | 📷 from a real device | 📷 from a real device | 📷 from a real PC |
| Icons / splash | ⏳ app-icons session | ⏳ app-icons session | ⏳ app-icons session |

✅ done in the repo · ⚠️ needs a value only you have · 🔑 only you · 📝 review · 📷 capture ·
⏳ another session · ❓ unverified here

---

## Shared across all stores

### Identity

| Property (`Trackify.csproj`) | Value | Verdict |
|---|---|---|
| `ApplicationId` | `com.ktechen.trackify` | Not a template placeholder (that would be `com.companyname.…`). It becomes the Android package name and the iOS bundle id, and **neither can change after the first upload**. Change it now if you would rather use a domain you own. |
| `ApplicationTitle` | `Trackify` | Home-screen and Start-menu name. Contains no trademark. ✅ |
| `Description` | was `Trackify powered by Uno Platform.` (template) | **Fixed:** `Züge per Bluetooth steuern und Gleispläne zeichnen.` Shown in the MSIX tile metadata. |
| `ApplicationPublisher` | `Trackify` | Uno turns it into the MSIX `Publisher="O=Trackify"` and `PublisherDisplayName`. That works for development only. The Store needs Partner Center's values ([Windows](#microsoft-store-windows)). |

**The store name is not free.** The App Store already lists apps called "Trackify" (a
work-hours logger, among others), and **App Store names have to be unique**. Google Play has a
"Trackify: GPS Location Tracker". So keep `Trackify` as the icon label, but give the listing a
distinct name, for example **"Trackify – Zugsteuerung"** / **"Trackify – Train Control"** (both
under 30 characters). Reserve it early, in App Store Connect and in Partner Center.

### Version

**The release pipeline owns the version.** Trackify uses Semantic Versioning: a Git tag `vX.Y.Z`
creates a release, and the pipeline passes the version into the build. The Android versionCode is
derived from the SemVer. This branch does not touch any version property. On master, the csproj
still hard-codes `ApplicationDisplayVersion 1.0` and `ApplicationVersion 1`, which makes the Windows
package `1.0.0.1`, and the Microsoft Store rejects that (see the last row).

What each store does with the two values, and the rule each one enforces:

| Store | From `ApplicationDisplayVersion` | From `ApplicationVersion` | Rule |
|---|---|---|---|
| Google Play | `versionName` (shown) | `versionCode` | versionCode must be higher than every earlier upload, including test tracks |
| App Store | `CFBundleShortVersionString` | `CFBundleVersion` | ShortVersion is `X.Y.Z`; the build number must grow within one version |
| Microsoft Store | MSIX `Major.Minor.Build` | **must not reach it**: revision 0 | The 4th part (revision) is reserved for the Store and **must be 0**. Each part ≤ 65535, and every upload must be higher |

**The last row is implemented by the release pipeline.** Uno.Resizetizer writes
`ApplicationVersion` into the MSIX revision (`1.0` + `1` → `1.0.0.1`). So the Windows build has to
end up with `ApplicationVersion` = 0, and the MSIX version is then `X.Y.Z.0`. Two consequences:
every Store upload needs a higher `X.Y.Z`, and a derived versionCode such as 10203 must not land in
the revision. It would be rejected, and it exceeds 65535 from major version 7 on. This was found
here, but it is implemented in one place only, the release pipeline.

### Trademarks (LEGO®, Powered Up)

LEGO and Powered Up are trademarks of the LEGO Group. Under the LEGO **Fair Play** guidelines the
word may be used only descriptively: as an adjective, with ®, never as the product's own name,
never with the LEGO logo, and never in a way that suggests the LEGO Group is behind the app.
Store reviewers apply the same standard: Apple guideline 5.2 and Google Play's impersonation and
intellectual-property policies.

| Where | Finding | Status |
|---|---|---|
| App name `Trackify`, bundle id, package name | No trademark | ✅ |
| iOS Bluetooth permission text | Said "LEGO Powered Up Hubs" as the object of the sentence | **Fixed:** "…die Hubs von Zügen (LEGO® Powered Up) …" |
| Android manifest, csproj | Code comments only, never shown | ✅ |
| UI: `MainPage.xaml` subtitle "Powered Up · Legoino-Schnittstelle" | Descriptive, but no ® and no disclaimer anywhere in the app | ⏳ UI work, see [open points](#open-points-outside-this-branch) |
| UI: `SecondPage.xaml` "Zeichnen · echte LEGO-Teile", `TrackCanvas.xaml` hint "…aus echten LEGO-Teilen" | Adjective use (fine), but no ® | ⏳ same |
| Icons / splash | Must not contain the LEGO logo, minifigures, the red square or brick studs as a visual quote | ⏳ app-icons session, please check |
| Store texts | The drafts below use the mark descriptively, never in the title, and always end with the disclaimer | 📝 |
| App Store keywords | **No** `LEGO`, `Powered Up`, `Technic` or `Duplo` in the keyword field (Apple 2.3.7) | 📝 drafts comply |
| Screenshots | Your own trains are fine. Crop out LEGO packaging, box art and building instructions | 📷 |

Disclaimer for every store description and for an in-app "Über" (About) screen:

> **DE:** LEGO® und Powered Up sind Marken der LEGO Gruppe, die diese App weder sponsert noch
> autorisiert oder unterstützt. Trackify ist ein unabhängiges Projekt.
>
> **EN:** LEGO® and Powered Up are trademarks of the LEGO Group, which does not sponsor, authorize
> or endorse this app. Trackify is an independent project.

### What the app does with data (the basis for every privacy answer)

Derived from the code on 2026-10-21. Re-check this list before each release that adds a feature.

- **Bluetooth LE** (Direct mode): scans for nearby Powered Up hubs and connects to the one you
  confirm. It stores the hub's name, type and BLE address/ID in the local SQLite store
  (`trackify.db`) **on the device**. Nothing leaves the device.
- **Location** (Android 6–11 only): Android required the location permission for any BLE scan
  then. The app never reads a location. From Android 12 on, the scan permission carries
  `neverForLocation`.
- **Local storage**: trains, track plans (SQLite), the Direct/Server choice and the server URL (app
  settings). Not backed up: `allowBackup=false` on Android.
- **Server mode**: the app sends train configuration and drive commands to the **URL you enter**,
  which is your own Trackify CLI backend, typically a Raspberry Pi. The developer never receives
  any of it.
- **None of these:** accounts, analytics, crash reporting, advertising, tracking, in-app purchases
  or third-party SDKs that phone home. Logging goes to the device console only (Serilog console
  sink).

---

## Google Play (Android)

### Audit

| Item | Found | Status |
|---|---|---|
| Target API | `net10.0-android` → targetSdk **36** (merged manifest), compileSdk 36 | ✅ Play requires API 36 for new apps and updates since 2026-08-31 |
| Minimum API | `SupportedOSPlatformVersion` 21 (Android 5.0) | ✅ (the legacy BLE permissions below cover 21–30) |
| `BLUETOOTH_SCAN` | declared with `neverForLocation` | ✅ |
| `BLUETOOTH_CONNECT` | declared | ✅ |
| `BLUETOOTH`, `BLUETOOTH_ADMIN`, `ACCESS_FINE_LOCATION` | `maxSdkVersion="30"`, so legacy devices only | ✅ |
| Runtime request | `MainActivity.EnsureBluetoothPermissionsAsync`: SCAN+CONNECT on 31+, FINE_LOCATION on 23–30, on demand from the scan | ✅ (the system dialog supplies the text, so Android has no usage string to write) |
| `uses-feature bluetooth_le` | `required="true"`, which hides the app on devices without BLE | ✅ intended |
| `INTERNET` | **missing**. .NET for Android adds it only to Debug builds, so Server mode would fail in every Play build | **Fixed:** declared |
| Package format | Release defaults to `AndroidPackageFormats=aab;apk` | ✅ the `.aab` is what Play takes |
| Release build | **Failed to restore** on SDK 10.0.2xx with Android workload 36.1.2 (`NU1102 Microsoft.NETCore.App.Runtime.Mono.win-x64`): with `PublishTrimmed` on, the SDK adds the host RID | **Fixed** in the csproj (`UseDefaultPublishRuntimeIdentifier=false`, which workload ≥ 36.1.30 does by itself, dotnet/android#10722). Alternatively run `dotnet workload update` |
| 16 KB page size | Enforced for apps targeting Android 15+ | see [verification](#verification-on-this-branch) |
| Cleartext HTTP | `usesCleartextTraffic=false`, so Server mode cannot reach `http://…` backends | ⚠️ intentional, but the UI's example is `http://192.168.1.50:5000`, see [open points](#open-points-outside-this-branch) |
| Backup | `allowBackup=false` | ✅. Optional: `dataExtractionRules` would also exclude device-to-device transfer on Android 12+ |
| App label | `@string/ApplicationName` = "Trackify" | ✅ (`Strings.xml` still has the template's unused `Hello` string, which is harmless) |

### Only you can do this

1. **Create the Play Console account** (one-time US$ 25, identity verification). On a **personal**
   account created after 2023-11-13, production stays locked until a **closed test has run with at
   least 12 testers who stayed opted in for 14 consecutive days**. Start that test as early as
   possible. An organisation account (needs a D-U-N-S number) is exempt.
2. **Create the upload key** and keep it outside the repo:
   ```bash
   keytool -genkeypair -v -keystore trackify-upload.keystore -alias trackify -keyalg RSA -keysize 2048 -validity 10000
   ```
   Enrol in **Play App Signing**, the default for new apps: Google holds the app signing key, and
   you only ever sign uploads.
3. **Host the privacy policy** at a public HTTPS URL. GitHub Pages works. Start from the
   [draft](#privacy-policy-draft) and enter the URL in Play Console → App content.
4. Fill in **App content**:
   - *Data safety*: the [answers below](#data-safety-form-proposed-answers).
   - *Ads*: no ads. *App access*: no login. *Target audience*: **13+ or 18+**, not "children". A
     children's audience pulls in the Families policy. *Content rating* (IARC questionnaire):
     nothing objectionable, so expect "USK 0 / PEGI 3 / Everyone".
   - *Permissions declaration*: Play may ask about `ACCESS_FINE_LOCATION`. Answer: only up to
     Android 11, only because the OS ties BLE scanning to it, and no location is ever read.
5. **Store listing**: name, short and full description (DE + EN [drafts](#store-listing-drafts)),
   app icon 512×512 and feature graphic 1024×500 (the app-icons session), 2–8 phone screenshots,
   and tablet screenshots if you want the app shown as tablet-ready.
6. Upload the signed `.aab` to the closed-testing track. Promote it to production after 14 days.

### Data safety form (proposed answers)

| Question | Proposed answer | Why |
|---|---|---|
| Does your app collect or share any of the required user data types? | **No** | Direct mode keeps hub addresses, trains and plans on the device, which Google exempts as on-device processing. Server mode sends train configuration and drive commands only to the URL the user typed in, their own server. The developer never receives or can access any of it. |
| Location | **Not collected** | Permission only on Android ≤ 11 for BLE scanning, and never read |
| Can users request deletion? | n/a (nothing collected). Uninstalling, or clearing the app data, deletes everything | |

**Your call, and a judgement:** Google defines "collect" as *any* transmission off the device,
regardless of the recipient. The form has no exemption that names user-operated servers. The
"No" above leans on the closest one: direct uploads to storage the user controls (Google's
Drive/Dropbox FAQ), where the app never collects or accesses the data. That describes Server mode.
The zero-risk alternative is to declare *App activity → Other user-generated content* (train and
track names) as collected, optional (Server mode only), not shared, for *App functionality*. The
listing would then show "data collected", though, and contradict "collects no data" in the
description. Apple's definition is narrower (data the developer can access), so the App Store
answer is a clean **Data Not Collected** either way. Re-answer both forms if analytics, crash
reporting or an account system is ever added.

### What CI needs (for the release-pipeline task, not built here)

- An **`.aab`** from the `vX.Y.Z` tag build: `dotnet publish Source/Trackify/Trackify.csproj -c
  Release -f net10.0-android -p:AndroidPackageFormats=aab` (or `aab;apk` to also keep a
  side-loadable APK), with the version from the tag. An APK alone cannot go to Play.
- Signing with the upload key: `-p:AndroidKeyStore=true -p:AndroidSigningKeyStore=<path>
  -p:AndroidSigningKeyAlias=trackify -p:AndroidSigningStorePass=env:… -p:AndroidSigningKeyPass=env:…`,
  with the keystore decoded from a repository secret.
- `dotnet workload install android` on the runner pulls a current workload, so the restore
  workaround is not needed there.
- Optional: upload to the internal or closed track with the Play Developer API (a service account
  JSON as a secret), for example `r0adkll/upload-google-play`.

---

## Apple App Store (iOS)

**Unverified here.** iOS needs a Mac with Xcode 26: the build, the simulator test, signing and
the upload. Everything in this section was checked by reading and by parsing the plists
(`plistlib`), not by building.

### Audit

| Item | Found | Status |
|---|---|---|
| SDK | `net10.0-ios` → iOS SDK **26.1** | ✅ Apple requires Xcode 26 / iOS 26 SDK for uploads since 2026-04-28. Build on a Mac with Xcode 26 |
| Minimum iOS | `SupportedOSPlatformVersion` 14.2 | ✅ |
| `NSBluetoothAlwaysUsageDescription` | present, said "LEGO Powered Up Hubs" | **Reworded:** "Trackify nutzt Bluetooth, um die Hubs von Zügen (LEGO® Powered Up) in der Nähe zu finden und Fahrt und Licht zu steuern." |
| `NSLocalNetworkUsageDescription` | **missing**. Server mode to a Pi on the LAN raises iOS's local-network prompt | **Added** |
| `ITSAppUsesNonExemptEncryption` | commented out, so App Store Connect asks on every build | **Set to `false`**: the only encryption is TLS through the OS networking stack, which is exempt |
| `CFBundleDevelopmentRegion` | missing, so iOS assumed English | **Set to `de`** |
| `UIRequiredDeviceCapabilities` | `armv7` + `arm64`. armv7 is a 32-bit template leftover, the binary is arm64-only | **Now** `arm64` + `bluetooth-le` (every iOS 14.2 device has BLE, so nobody is excluded) |
| `PrivacyInfo.xcprivacy` | **Malformed**: four `<dict>`s at the root and no `NSPrivacyAccessedAPITypes` key. A plist parser keeps one dict and sees no API declarations, so every required-reason API counts as undeclared (ITMS-91053) | **Rewritten**: tracking `false`, no tracking domains, no collected data; FileTimestamp C617.1, SystemBootTime 35F9.1, DiskSpace E174.1, UserDefaults CA92.1 |
| Entitlements | empty | ✅ BLE needs none. No background mode, because the app only controls hubs while open |
| iPad | `UIDeviceFamily` 1+2, all four iPad orientations | ✅ but iPad screenshots then become mandatory |
| App Transport Security | defaults, no exceptions | ⚠️ whether Server mode reaches a plain `http://` Pi on iOS must be tested on a device. If it doesn't, either serve the backend over HTTPS or add `NSAllowsLocalNetworking` (allowed without justification) |
| Icon (`XSAppIconAssets`) | | ⏳ app-icons session |

### Only you can do this

1. **Join the Apple Developer Program** (US$ 99 a year). In App Store Connect, **declare your
   EU trader status** (Digital Services Act). As a trader, your address and phone number appear
   publicly on the product page. Without the declaration the app is not distributed in the EU.
2. **On a Mac** (Xcode 26 + the .NET 10 iOS workload): let Xcode manage the distribution
   certificate and an App Store provisioning profile for `com.ktechen.trackify`, then
   `dotnet publish Source/Trackify/Trackify.csproj -c Release -f net10.0-ios -r ios-arm64`
   and upload the `.ipa` with Transporter or `xcrun altool`.
3. **Create the app record** in App Store Connect under a unique name (see
   [Identity](#identity)). Primary language German, category *Utilities* (or *Entertainment*).
4. **App Privacy**: "Data Not Collected". **Age rating**: 4+. **Export compliance**: answered by
   the plist. **Privacy Policy URL**: required.
5. **Review notes**: the reviewer has no hub. Say so, and attach a short video of a real train
   being discovered and driven. Without it, a Bluetooth-only app tends to get "we could not test
   the core function" (guideline 2.1).
6. **TestFlight** first, then submit. Screenshots: iPhone 6.9″ (1320×2868 or 1290×2796) and
   iPad 13″ (2064×2752 or 2048×2732).

### What CI needs

A macOS runner with Xcode 26, the App Store Connect API key (`.p8` + key id + issuer id) as
secrets, and the distribution certificate + profile (or automatic signing via the API key). Upload
with `xcrun altool --upload-app` or `fastlane pilot`.

---

## Microsoft Store (Windows)

### Audit

| Item | Found | Status |
|---|---|---|
| Identity `Name` | generated from `ApplicationId` → `com.ktechen.trackify` | ⚠️ Partner Center assigns its own (for example `12345YourName.Trackify`), so it must be copied in |
| Identity `Publisher` | generated `O=Trackify` | ⚠️ must be Partner Center's `CN=…` exactly, or the upload is refused |
| `PublisherDisplayName` | generated `Trackify` | ⚠️ must match your Partner Center publisher name |
| Version | `1.0.0.1` (revision = `ApplicationVersion`), rejected by the Store | ⏳ the release pipeline sets revision 0 (see [Version](#version)) |
| Description | template "Trackify powered by Uno Platform." | **Fixed** (csproj `Description`) |
| Bluetooth capability | **missing** | **Added** `<DeviceCapability Name="bluetooth" />` |
| Network capabilities | missing | **Added** `internetClient` + `privateNetworkClientServer` (Server mode) |
| `runFullTrust` | present | ✅ standard for a WinAppSDK desktop app. Partner Center asks for a one-line justification: "Desktop app built on the Windows App SDK" |
| Target devices | `Windows.Universal` + `Windows.Desktop`, min 10.0.17763 | ✅ (the WinUI 3 template default). Partner Center → Availability: Desktop only |
| Declared languages | `<Resource Language="x-generate"/>` + the template's `Strings/en|es|fr|pt-BR` | ⚠️ the package will probably declare English, Spanish, French and Portuguese, and not German. See [open points](#open-points-outside-this-branch) |
| Tile/logo/splash entries | | ⏳ app-icons session |

### Only you can do this

1. **Register in Partner Center** as an individual developer. This has been free since
   September 2025 (company accounts still pay). Use storedeveloper.microsoft.com.
2. **Reserve the app name**. Then copy from *Product identity* into `Package.appxmanifest`:
   ```xml
   <Identity Name="12345YourName.Trackify" Publisher="CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" />
   <Properties><PublisherDisplayName>Your publisher name</PublisherDisplayName></Properties>
   ```
   Values written there win over the generated ones. Leave `Version` out, because the build sets
   it from the release tag.
3. **Take the Store upload** (`.msixupload`, unsigned is fine because the Store signs it) from the
   release pipeline's tag build. For reference, the local equivalent (not run here) is:
   ```bash
   dotnet publish Source/Trackify/Trackify.csproj -c Release -f net10.0-windows10.0.19041.0 -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=StoreUpload -p:AppxBundle=Always -p:AppxBundlePlatforms="x64|arm64" -p:ApplicationDisplayVersion=X.Y.Z -p:ApplicationVersion=0
   ```
4. **Run the Windows App Certification Kit** (`appcert.exe`, from the Windows SDK) on the
   package, then submit with: privacy policy URL, age rating questionnaire (IARC), category
   *Utilities & tools*, at least one screenshot (1920×1080 recommended), DE + EN listing.

### What CI needs

`windows-latest`, the command from step 3 with the version from the tag, and the Partner Center
values. Either commit them to the manifest (they are not secret) or inject them with `-p:`. The
Windows build has to get `ApplicationVersion=0`, never the derived versionCode (see
[Version](#version)).
Signing is only needed for the side-load MSIX: a `.pfx` whose subject equals the manifest's
`Publisher`. Store submission can be automated with the Microsoft Store Developer CLI
(`msstore publish`) and an Entra ID app registration.

---

## Store requirements (checked on 2026-10-21)

| Requirement | Date | Source |
|---|---|---|
| Google Play: new apps and updates must target Android 16 (API 36) | 2026-08-31 (extension possible to 2026-11-01) | [Play target-API requirement](https://developer.android.com/google/play/requirements/target-sdk) |
| Google Play: 16 KB memory page support for apps targeting Android 15+ | enforced since 2025-11-01 (extensions ran out in 2026) | [Android 16 KB page sizes](https://developer.android.com/guide/practices/page-sizes) |
| Google Play: new personal accounts need a 12-tester / 14-day closed test | since 2023-11-13 (12 testers since late 2024) | Play Console Help, "App testing requirements for new personal developer accounts" |
| App Store: uploads must be built with Xcode 26 / an iOS 26 SDK | 2026-04-28 | [Apple upcoming requirements](https://developer.apple.com/news/upcoming-requirements/) |
| App Store: privacy manifest + required-reason APIs | since 2024-05-01 | [Privacy manifest files](https://developer.apple.com/documentation/bundleresources/privacy-manifest-files) |
| App Store: EU trader status declared | since 2025-02-17 | App Store Connect Help, "Manage European Union Digital Services Act trader requirements" |
| Microsoft Store: free registration for individual developers | 2025-09-10 | [Windows Developer Blog](https://blogs.windows.com/windowsdeveloper/2025/09/10/free-developer-registration-for-individual-developers-on-microsoft-store/) |

---

## Screenshots

These cannot be captured here: the Skia surface renders blank and the WASM canvas times out (see
`CLAUDE.md`). They have to come from real hardware. Suggested set, the same four motifs in every
store:

1. Train list with a connected hub, speed slider mid-way
2. Hub discovery, with a found hub offered for confirmation
3. Track planner, an oval with a station and a switch, one part selected
4. Connection panel, Server mode

| Store | Required sizes | Count |
|---|---|---|
| Google Play | phone, 16:9 or 9:16, 1080×1920 recommended; feature graphic 1024×500 | 2–8 (+ optional 7″/10″ tablet) |
| App Store | iPhone 6.9″ 1320×2868 (or 1290×2796); iPad 13″ 2064×2752 (or 2048×2732) | 1–10 each |
| Microsoft Store | 1920×1080 or larger, desktop | 1–10 |

Suggested filenames: `docs/images/store/<store>-<n>-<motif>.png` (for example
`docs/images/store/play-1-zugliste.png`). That keeps them apart from the editor screenshots that
`docs/images/.gitkeep` describes.

---

## Store listing drafts

To review. The function claims match the code on 2026-10-21. **Check which hub types you have
actually tested** before keeping the hub line. The catalogue offers City Hub, Technic Hub, Move Hub,
Duplo Train Hub, Remote and WeDo 2.0.

### German (primary)

**App name (≤ 30):** Trackify – Zugsteuerung

**Short description (Play, ≤ 80):** Züge per Bluetooth fahren und Gleispläne aus echten Gleisteilen zeichnen.

**Subtitle (App Store, ≤ 30):** Züge fahren, Gleise planen

**Keywords (App Store, ≤ 100 bytes):** zug,eisenbahn,bluetooth,hub,gleisplan,gleis,steuerung,lok,weiche,fahrregler,spielzeugbahn

**Description:**

> Trackify steuert deine Züge mit Powered-Up-Hubs (LEGO®) direkt per Bluetooth – ohne Konto,
> ohne Cloud, ohne Werbung.
>
> ZÜGE EINRICHTEN
> • Hubs in der Nähe suchen und nach deiner Bestätigung als Zug übernehmen
> • Pro Zug Name, Hub-Modell, Belegung der Anschlüsse und Lichtfarbe festlegen
> • Anfahren und Bremsen über eine einstellbare Kurve
> • Verbinden, mit dem Schieberegler fahren, trennen
>
> STRECKE PLANEN
> • Vom leuchtenden Endpunkt ziehen – die Strecke entsteht aus echten Gleisteilen
> • Geraden, Kurven, Weichen, Kreuzungen und Bahnhöfe
> • Lücken schließen: Trackify sucht die passende Teilefolge
> • Pro Abschnitt Tempo, Fahrtrichtung, Weichenstellung und Sensor festlegen
>
> SERVERMODUS
> Statt über das Bluetooth dieses Geräts lässt sich über einen eigenen Trackify-Server steuern,
> zum Beispiel einen Raspberry Pi an der Anlage. Die Server-Software ist Teil des Projekts.
>
> DEINE DATEN BLEIBEN BEI DIR
> Züge und Gleispläne werden nur auf deinem Gerät gespeichert. Trackify sammelt keine Daten,
> zeigt keine Werbung und braucht kein Konto.
>
> Voraussetzungen: ein Hub der Powered-Up-Familie (LEGO®) und ein Gerät mit Bluetooth Low Energy.
> Die App ist auf Deutsch.
>
> LEGO® und Powered Up sind Marken der LEGO Gruppe, die diese App weder sponsert noch autorisiert
> oder unterstützt. Trackify ist ein unabhängiges Projekt.

### English

**App name (≤ 30):** Trackify – Train Control

**Short description (Play, ≤ 80):** Drive your trains over Bluetooth and draw track plans from real track pieces.

**Subtitle (App Store, ≤ 30):** Drive trains, plan tracks

**Keywords (App Store, ≤ 100 bytes):** train,railway,bluetooth,hub,track plan,track,controller,locomotive,switch,throttle,toy train

**Description:**

> Trackify drives your trains with Powered Up hubs (LEGO®) directly over Bluetooth – no account,
> no cloud, no ads.
>
> SET UP TRAINS
> • Scan for nearby hubs and add them as trains once you confirm
> • Set name, hub model, port assignment and light colour per train
> • Smooth acceleration and braking along an adjustable curve
> • Connect, drive with the speed slider, disconnect
>
> PLAN YOUR LAYOUT
> • Drag from a glowing end point – the track is built from real track pieces
> • Straights, curves, switches, crossings and stations
> • Close gaps: Trackify finds the run of pieces that fits
> • Set speed, direction, switch position and sensor per section
>
> SERVER MODE
> Instead of this device's Bluetooth, drive through your own Trackify server, for example a
> Raspberry Pi next to the layout. The server software is part of the project.
>
> YOUR DATA STAYS WITH YOU
> Trains and track plans are stored on your device only. Trackify collects no data, shows no ads
> and needs no account.
>
> Requirements: a hub from the Powered Up family (LEGO®) and a device with Bluetooth Low Energy.
> The app's interface is in German.
>
> LEGO® and Powered Up are trademarks of the LEGO Group, which does not sponsor, authorize or
> endorse this app. Trackify is an independent project.

---

## Privacy policy draft

A template to adapt, not legal advice. Fill in the bracketed fields, have it checked if you are
unsure, and host it at a stable HTTPS URL (GitHub Pages works). The same URL goes into all three
stores. If you distribute commercially (a trader under the EU DSA), German law may also require an
Impressum.

### Datenschutzerklärung für die App „Trackify“

**Stand:** [Datum]

**Verantwortlicher:** [Vor- und Nachname], [Anschrift], E-Mail: [Adresse]

**Kurz gesagt:** Trackify erhebt keine personenbezogenen Daten. Alles, was die App speichert,
bleibt auf deinem Gerät. Es gibt kein Konto, keine Werbung, kein Tracking und keine Analyse.

**1. Bluetooth.** Im Direktmodus sucht die App per Bluetooth Low Energy nach Zug-Hubs in der Nähe
und verbindet sich mit dem Hub, den du bestätigst. Name, Typ und Bluetooth-Adresse des Hubs
werden **nur lokal** auf deinem Gerät gespeichert, damit sich die App den Zug merkt. Diese Daten
werden nicht an uns oder Dritte übertragen.

**2. Standortberechtigung (nur Android 6 bis 11).** Auf diesen Android-Versionen verlangt das
Betriebssystem für jede Bluetooth-Suche die Standortberechtigung. Trackify fragt sie nur dort und
nur dafür an und **liest niemals deinen Standort aus**. Ab Android 12 ist die Bluetooth-Suche
ausdrücklich vom Standort getrennt.

**3. Lokal gespeicherte Daten.** Züge, Gleispläne und die Einstellungen (Direkt- oder
Servermodus, Serveradresse) liegen in einer Datenbank auf deinem Gerät. Unter Android sind sie
von der Cloud-Sicherung ausgenommen. Wenn du die App deinstallierst oder ihre Daten löschst, ist
alles entfernt.

**4. Servermodus (optional).** Wenn du den Servermodus einschaltest, sendet die App
Zugkonfigurationen und Fahrbefehle an die **Adresse, die du selbst einträgst**, also an deinen
eigenen Trackify-Server (zum Beispiel einen Raspberry Pi). Wir betreiben keinen Server und
erhalten diese Daten nicht. Für deinen Server bist du selbst verantwortlich.

**5. App-Stores.** Wenn du Trackify über Google Play, den App Store oder den Microsoft Store
lädst, verarbeitet der jeweilige Anbieter Daten nach seiner eigenen Datenschutzerklärung. Darauf
haben wir keinen Einfluss.

**6. Deine Rechte.** Weil wir keine personenbezogenen Daten über dich verarbeiten, gibt es bei
uns nichts auszukunften, zu berichtigen oder zu löschen. Fragen beantworten wir trotzdem gern
unter [E-Mail]. Du hast das Recht, dich bei einer Datenschutz-Aufsichtsbehörde zu beschweren.

**7. Änderungen.** Ändert sich, was die App mit Daten macht, aktualisieren wir diese Erklärung
und das Datum oben.

### Privacy policy for the "Trackify" app

**Last updated:** [date]

**Controller:** [full name], [address], email: [address]

**In short:** Trackify collects no personal data. Everything the app stores stays on your device.
There is no account, no advertising, no tracking and no analytics.

**1. Bluetooth.** In Direct mode the app scans for nearby train hubs over Bluetooth Low Energy and
connects to the hub you confirm. The hub's name, type and Bluetooth address are stored **only
locally** on your device so the app remembers the train. They are not sent to us or to anyone
else.

**2. Location permission (Android 6 to 11 only).** On these Android versions the operating system
requires the location permission for any Bluetooth scan. Trackify asks for it only there and only
for that reason, and **never reads your location**. From Android 12 on, Bluetooth scanning is
explicitly separated from location.

**3. Data stored locally.** Trains, track plans and settings (Direct or Server mode, server
address) are kept in a database on your device. On Android they are excluded from cloud backup.
Uninstalling the app or clearing its data removes everything.

**4. Server mode (optional).** If you switch on Server mode, the app sends train configurations
and drive commands to **the address you enter yourself**, which is your own Trackify server (for
example a Raspberry Pi). We run no server and never receive this data. You are responsible for
your server.

**5. App stores.** When you download Trackify from Google Play, the App Store or the Microsoft
Store, that provider processes data under its own privacy policy, which we do not control.

**6. Your rights.** Because we process no personal data about you, there is nothing for us to
disclose, correct or delete. We are still happy to answer questions at [email]. You have the right
to lodge a complaint with a data protection supervisory authority.

**7. Changes.** If what the app does with data changes, we will update this policy and the date
above.

---

## Open points outside this branch

These were found during the audit but belong to other work:

- **Template localisation leftovers** (`Strings/en|es|fr|pt-BR/Resources.resw` with values like
  `Trackify-es`, and `appsettings.json` → `LocalizationConfiguration.Cultures = es, fr, pt-BR, en`,
  without `de`). Nothing reads these strings, but they make the packages advertise four languages
  the app does not have, and leave out German. The Uno localisation feature may also pick `es` as
  the app culture on a German device. Fix: remove the four `.resw` files, and set the cultures to
  `de` (or drop `UseLocalization`). Then check number formatting on a device.
- **In-app trademark notice**: an "Über Trackify" (About) entry with the disclaimer above, plus ®
  on the visible "LEGO" mentions in `MainPage.xaml`, `SecondPage.xaml` and `TrackCanvas.xaml` (UI
  work).
- **Server mode over plain HTTP** cannot work on Android (`usesCleartextTraffic=false`), may not
  work on iOS, and the connection panel's example is `http://192.168.1.50:5000`. Either document
  HTTPS for the Pi backend (a reverse proxy) and change the example, or deliberately allow
  cleartext to local addresses only (Android `network_security_config`, iOS
  `NSAllowsLocalNetworking`).
- **Release pipeline** (its own session): version from the `vX.Y.Z` tag, a signed `.aab` for
  Play, the Store `.msixupload` with MSIX revision 0, and later the iOS job. See each "What CI
  needs" section.
- **Android 16 behaviour changes** that come with targetSdk 36: edge-to-edge can no longer be
  turned off, and orientation/resizability locks are ignored on screens ≥ 600 dp. Check both on a
  tablet before the first release.

---

## Verification on this branch

Results on 2026-10-21:

- Debug builds green: `net10.0-android`, `net10.0-desktop` and `net10.0-windows10.0.19041.0` with
  0 warnings, `net10.0-browserwasm` with the known `WASM0001` from `e_sqlite3`.
  `dotnet test Test/Trackify.Tests` 167/167.
- Android **Release** publish produced `com.ktechen.trackify-Signed.aab` + `.apk`. All 426 native
  libraries (arm64 + x86_64, including e_sqlite3, SkiaSharp and HarfBuzzSharp) have ≥ 16 KB
  `PT_LOAD` alignment.
- Android: the **merged** manifest in the build output
  (`obj/<config>/net10.0-android/android/manifest/AndroidManifest.xml`), not just the source file,
  in both Debug and Release: minSdk 21, targetSdk 36, the BLE permissions as intended, INTERNET
  present.
- Windows: the generated `obj/Debug/net10.0-windows10.0.19041.0/unoresizetizer/m/Package.appxmanifest`
  has the new capabilities and the real description. Its version is still `1.0.0.1` until the
  release pipeline sets revision 0.
- iOS: plists parsed with `plistlib` only, no build (needs a Mac).
- Not run: the Microsoft Store `.msixupload` command above.
