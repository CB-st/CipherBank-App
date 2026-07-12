# Handoff: Cipherbank — Consumer App + Marketing + Brand System

## Overview
Cipherbank is a self-custodied money-movement product that converts crypto, fiat, and (soon) securities into one balance. Users convert instantly, send by ACH, pay bills with a **mix** of assets, and receive — all privately. The product also ships as an **open-source API standard** so banks and developers can embed crypto accounts and instant conversion into their own apps.

This package contains the full v1 design system: brand tokens, a logo/asset library, a 7-screen mobile app, desktop + mobile marketing pages, and a build spec. The mascot/digital teller is **Cora Byte** (initials C.B.) — serious about money, with a dry sense of humor she sneaks into one line per screen.

## About the Design Files
The files in `designs/` are **design references authored in HTML** (as "Design Components" — self-contained `.dc.html` files). They are prototypes showing the intended look, copy, and behavior — **not production code to copy directly**.

Your task is to **recreate these designs in the target codebase's environment** using its established patterns and libraries. If no environment exists yet, choose the most appropriate stack (the app is mobile-first — React Native / Expo, SwiftUI, or a React PWA are all reasonable) and implement there. The `.dc.html` files depend on a small runtime (`support.js`) and helper components (`ios-frame.jsx`, `image-slot.js`, `doc-page.js`) that are included only so the references render locally in a browser — **do not port the runtime; port the UI.**

To view a reference: open any `designs/*.dc.html` in a browser.

## Fidelity
**High-fidelity.** Final colors, typography, spacing, radii, shadows, and copy are all intentional and specified below. Recreate the UI pixel-close using the codebase's component library. Exact values are in `tokens/tokens.css` and `tokens/tokens.json`.

## Design Tokens
Machine-readable: `tokens/tokens.css` (CSS custom properties, `--cb-*`) and `tokens/tokens.json`.

### Color
| Token | Hex | Role |
|---|---|---|
| gold | `#F2C14E` | **Primary action / accent.** One gold button per view. |
| goldDark | `#C9971F` | Gold on light surfaces (contrast). |
| violet | `#7B4DFF` | Interactive text, links, secondary highlights. |
| deepPurple | `#2B1E3E` | Dark surface (Cora bars, hero panels). |
| ink | `#111318` | Near-black background / primary text. |
| inkDeep | `#0C0D11` | Footer / deepest background. |
| canvas | `#F7F5F2` | Off-white app canvas (all app screens). |
| green | `#3FA46A` | Positive / gains / success. |
| red | `#C0574B` | Negative / loss. |
| textMuted | `#5A5563` | Body text on light. |
| textSubtle | `#8A8496` | Captions, metadata. |
| hairline | `#F0EDEA` | Row dividers on white. |

**Rule:** Gold is the only primary-action color — never use it for body text on white. Violet is for interactive/link text.

### Typography
- **Display** — Space Grotesk 700/800. Headings, balances, logo wordmark. Tight tracking (−0.5px to −2.5px).
- **Body** — Manrope 400–800. All UI copy, labels, buttons.
- **Mono** — Space Mono 400/700. Amounts, ticker labels, metadata, code, timers, recovery phrases.

Key sizes: balance hero 40px/700/−1.5px · card title 22px/700/−0.5px · body 15px/1.55 · button 16px/800 · mono meta 12px.

### Radius & Shadow
Radius: chip 10 · button 14 · card 18 · panel 22 · pill 30 (px).
Shadow: card `0 2px 10px rgba(0,0,0,.05)` · gold button `0 8px 20px rgba(242,193,78,.35)` · floating chip `0 14px 34px rgba(0,0,0,.4)`.

### Spacing
4 · 8 · 14 · 18 · 22 · 30 (px).

## Assets
All in `assets/` (also indexed machine-readably in `assets/manifest.json`).

- **Logos** (`assets/logo/`): `cipherbank-logo-dark.svg`, `cipherbank-logo-light.svg`, `cipherbank-mark.svg` (CB diamond), `cipherbank-app-icon.svg` (180px), `cipherbank-favicon.svg` (32px).
- **Currency glyphs** (`assets/icons/asset-*.svg`, 44px): BTC ₿, ETH Ξ, DOGE Ð, XMR ɱ, LTC Ł, USD $, EUR €, JPY ¥. Tint = 14% of brand hue; glyph = stronger shade.
- **UI icons** (`assets/icons/ui-*.svg`, 24px stroke): convert, send, pay, receive, shield, home, activity.
- **Mascot (Cora Byte):** NOT included — the designs use drag-and-drop `image-slot` placeholders labeled "Cora". The client will supply transparent-PNG cutouts. In code, treat Cora as an `<Image>` slot (circle avatar 42–46px on screens; larger rounded pose on onboarding/hero).

> The CB diamond mark is currently a geometric stand-in (gold rotated-square outline enclosing a violet square). Swap in the final mark by replacing the SVGs — paths/filenames can stay.

### Supported currencies (data model)
crypto: BTC, ETH, DOGE, XMR (always "shielded"), LTC · fiat: USD (instant ACH), EUR, JPY · securities: **coming soon** (tag "NEW" until GA). Model assets generically: `{ symbol, name, glyph, type: crypto|fiat|security, note? }`.

## Screens / Views
All app screens are **390px-wide mobile**, off-white (`canvas`) background, Manrope body, with a persistent bottom tab bar (Home · Convert · Pay · Activity · Profile) except onboarding. Each screen carries a **Cora bar**: deep-purple `#2B1E3E` rounded row, circular Cora avatar + one dry line (gold mono eyebrow "CORA BYTE" on Home).

Reference file: `designs/Cipherbank App.dc.html` (screens numbered 01–07 across the row).

### 01 · Home / Accounts (OP-04)
- **Purpose:** Landing screen; overview of all holdings.
- **Layout:** Header (logo + bell) → Cora bar → balance hero → quick-actions row → assets list → tab bar.
- **Components:** Balance hero is a `deepPurple`→`#191026` gradient panel (radius 22), total in Space Grotesk 40px, green change pill (`#3FA46A22` bg / `#5fce8f` text), meta chips ("8 assets", "3 currencies", gold "self-custodied"). Quick actions: 4 items, first is a gold 54px rounded-square (convert icon) with gold shadow, rest white cards. Assets list: white card (radius 18), each row = 36px tinted glyph + name + mono sub + right-aligned USD value + % change (green/red). Footer teaser: "Securities coming soon — pay with stock." Cora line: *"Rates move all day. Your privacy doesn't. That's the deal."*

### 02 · Instant Convert (OP-01)
- **Purpose:** Swap crypto ⇄ fiat at a locked rate.
- **Layout:** Back header → Cora bar → From card / swap FAB / To card → rate-lock strip → breakdown → CTA.
- **Components:** From = white card; To = `deepPurple` card. Amounts in Space Grotesk 36px. Asset selector = pill with glyph + symbol + chevron. Centered 44px gold **swap FAB** overlapping both cards (4px canvas border). Rate strip: gold-tint bg, "1 BTC = $63,204.18" + "● Rate locked · 12s" countdown pill. Breakdown rows: Network fee `$0.00 we cover it`, Privacy `Shielded swap`, Settlement `Instant`. CTA: gold "Convert instantly". Cora line: *"Locked-in rate. No spread games, no surprises."*

### 03 · Pay With A Mix (OP-02) — the differentiator
- **Purpose:** Pay a bill/recipient using several funding sources at once.
- **Layout:** Back header → recipient card → funding-mix stacked bar → sources list → mediation note → Cora bar → CTA.
- **Components:** Recipient card: 44px monogram tile (`deepPurple`/gold) + name + "Rent · due Jul 1", amount `$2,400.00` centered 42px. **Funding mix bar:** horizontal stacked segments (gap 2px, radius 8) colored per source, must total 100%. Sources list (white card): each row = 32px glyph + name + mono amount-in-asset + right USD value. Securities row (AAPL) carries a gold "NEW" pill. Mediation note: violet-tint callout — "We mediate the exchange in real time. Sunset receives clean USD — they never see the mix." Cora line: *"Rent, paid partly in Dogecoin. Bold. Also completely fine."* CTA: gold "Pay $2,400.00".

### 04 · Send / ACH (OP-03)
- **Purpose:** Send money to a person/bank, instant or standard ACH.
- **Layout:** Back header → recipient card → amount card → speed toggle → breakdown → Cora bar → CTA.
- **Components:** Recipient: 44px circle initials + "Maya Chen" + mono "Chase ••4021 · ACH". Amount card: `deepPurple`, 46px amount, "From USD balance" pill. **Speed toggle:** segmented control in `#eceae6`, active = gold ("Instant") vs "Standard ACH". Breakdown: Arrives `Instantly · Cipherbank rail`, Fee `$0.00`, Privacy `They see a handle, not you`. Cora line: *"Instant. As it should've been all along."* CTA: gold "Send $1,200.00".

### 05 · Receive (OP-05)
- **Purpose:** Receive into a chosen asset via QR/handle.
- **Layout:** Back header → asset selector (4 tabs: BTC active, ETH, USD, More) → QR card → action row → Cora bar.
- **Components:** QR card: `deepPurple`, white 172px rounded QR containing the CB mark at center, handle row "bc1q · cora@cipherbank.id" with copy icon. Action row: white "Request amount" + gold "Share". Cora line: *"They see the handle. Not you. That's the point."* (QR in the reference is decorative — generate a real one in code.)

### 06 · Onboarding — Welcome (OP-06, step 1)
- **Purpose:** Introduce Cora + value prop; start account creation.
- **Layout:** `deepPurple`→`#17101f` gradient full screen. Logo → large Cora pose (rounded slot, radius 24, violet radial glow behind) → gold mono eyebrow → headline "Money in any form. Yours to keep." → subcopy → 4-dot progress (first gold) → gold "Create my account" → "Already have one? Sign in".
- Cora intro copy: *"I'm Cora. I move your crypto, cash, and — soon — stocks as one balance. Serious about your money. Dry about most other things."*

### 07 · Onboarding — Secure Keys (OP-06, step 2)
- **Purpose:** Self-custody key generation + recovery phrase.
- **Layout:** Back header + "Step 2 of 4" → 4-seg progress (2 gold) → shield-check icon tile → "Your keys. Your money." → explainer → recovery-phrase grid → "Copy phrase" → Cora bar → CTA.
- **Components:** Recovery grid: white card, 2-col, numbered mono chips (`#F7F5F2` bg, radius 10). Cora line: *"No 'forgot password' button here. That's a feature, not an oversight."* CTA: gold "I've saved it — continue". Icon tile: `deepPurple` rounded, gold shield-check.

### Marketing — Desktop (`designs/Cipherbank Landing.dc.html`, 1440px)
Sticky blurred nav → hero (radial-purple bg, H1 "Move money in any form. Keep it yours." with gold/violet emphasis, dual CTA, floating BTC-converted + ACH-settled chips over a Cora hero slot, 9-item asset ticker) → "How it works" 3-card grid on canvas → "For Banks & Developers" `deepPurple` section with a mac-style **API code card** (`POST /v1/convert`) → Cora quote strip → 4-column footer.

### Marketing — Mobile (`designs/Cipherbank Landing Mobile.dc.html`, 390px)
Same content, single column: hamburger nav, stacked CTAs, wrapped ticker, stacked feature cards, code card, centered Cora strip, condensed footer.

## Interactions & Behavior
- **Convert:** pick from-asset + amount → live fiat estimate → pick to-asset → **rate locks with visible countdown** (re-lock on expiry) → confirm → success (updated balances). States: empty · rate-locked · rate-expired · insufficient · success.
- **Pay with a mix:** select recipient/amount → add funding sources, each editable, stacked bar reflects composition and must reach 100% → confirm → per-source settlement receipt. States: under-funded (<100%) · over-funded · single-source · multi-source · success.
- **Send:** choose recipient (handle or linked bank) → amount + source (auto-converts cross-asset first) → toggle Instant/Standard → review → send → confirmation. States: instant · standard · cross-asset · pending · settled.
- **Receive:** pick asset → show handle + QR → optional request amount → share sheet. States: default · amount-requested · received toast.
- **Onboarding:** welcome → key-gen + recovery confirm + biometric lock → optional bank link (skippable) → fund → Home. States: welcome · key-gen · recovery-confirm · bank-link · funded · skipped.
- **Home:** rotating Cora one-liner; privacy toggle hides values (eye icon present in hero). States: funded · new/empty · value-hidden · loading.
- **Transitions:** floating hero chips use a 6s ease-in-out `translateY(±8–10px)` idle bob. Gold CTAs lift on hover/press.

## State Management
- **Portfolio:** array of holdings `{ asset, amountInAsset, usdValue, change24h }`; derived total USD, asset count, currency count.
- **Convert:** `fromAsset, toAsset, amount, lockedRate, lockExpiresAt` (countdown timer), `status`.
- **Pay:** `recipient, total, sources[] ({asset, value})`, derived `coverage %` (block confirm until ≥100%).
- **Send:** `recipient, amount, sourceAsset, speed (instant|ach), status`.
- **Onboarding:** `step (1–4), recoveryConfirmed, bankLinked, funded`.
- **Privacy:** `valuesHidden` boolean (Home).
- **Cora:** per-screen line is static copy today; Home line can rotate from a small pool.
- **Data:** live rates (poll/stream for the lock countdown), balances, recipients/handles, ACH bank links. Conversion + mix mediation happen server-side; UI just renders quotes and settlement.

## Files
**Start here:** `starter/AGENTS.md` — how to build the app, with the exact place to start interfacing with an API. `starter/API.md` — the full service/endpoint spec to build server-side. `ARCHITECTURE.md` — file layout, component tree, and the full UI↔backend async contract (time-of-flight loading, rate-lock, optimistic settlement). `designs/Cipherbank Prototype.dc.html` is the clickable reference for those patterns.

The `starter/` folder is a runnable-shaped Expo + React Native + TypeScript scaffold (providers, navigation, theme from tokens, primitives, feature hooks, screen stubs) — the real repo to build in.

Design references (open in a browser):
- `designs/Cipherbank Prototype.dc.html` — **interactive** prototype: tab nav, async skeleton loading, live rate-lock countdown, optimistic pending→settled toasts
- `designs/Cipherbank App.dc.html` — 7 app screens (01–07)
- `designs/Cipherbank Landing.dc.html` — desktop marketing (1440px)
- `designs/Cipherbank Landing Mobile.dc.html` — mobile marketing (390px)
- `designs/Cipherbank Build Spec.dc.html` — printable spec (tokens, asset library, step-by-step operations, build order)
- `designs/Cipherbank Assets.dc.html` — asset gallery (reads `assets/manifest.json`)
- Helper/runtime (reference-only, do not port): `designs/support.js`, `designs/ios-frame.jsx`, `designs/image-slot.js`, `designs/doc-page.js`

Tokens & assets:
- `tokens/tokens.css`, `tokens/tokens.json`
- `assets/` (logos, icons) + `assets/manifest.json`

## Suggested Build Order
1. **Foundations** — tokens, fonts (Space Grotesk / Manrope / Space Mono), logo, asset-glyph + UI-icon components.
2. **Home (04)** — the shell everything returns to.
3. **Convert (01)** — core promise; its quote/lock logic is reused by Send & Pay.
4. **Send (03)** & **Receive (05)** — money in/out.
5. **Pay with a mix (02)** — the differentiator; depends on Convert.
6. **Onboarding (06/07)** — wrap once there are flows to land in.
7. **Securities** — extend the asset model to unlock "pay/invest with anything."
