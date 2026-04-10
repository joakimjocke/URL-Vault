Brand Identity: The Scruffy Engineer

Tagline: Curious by Design

Den här profilen är byggd för en ingenjör och systemarkitekt med en karaktäristisk rufsig stil, hög kompetens, nyfikenhet och problemlösningsfokus. Den funkar för webb, YouTube, GitHub, streaming, logotyper, banners, presentationsmaterial och mer.

## 🧭 1. Brand Essence
Kärnvärden
- Nyfikenhet
- Ingenjörsmässig precision
- Ödmjuk briljans
- Kreativ problemlösning
- Autenticitet

Personlighet
- Smart men avdramatiserad
- Lekfull i formen, skarp i sak
- Tekniskt djup + mänsklig värme
- Lite kaotisk, väldigt kunnig
- “Maker + Architect + Explorer”

Positionering
En erfaren, jordnära ingenjör som förklarar komplex teknik, bygger system, experimenterar och delar insikter — med charm, transparens och klurighet.

## 🎨 2. Färgpalett

Den ursprungliga paletten finns kvar som grund, men är nu utökad för att spegla implementationen i appen, inklusive dark mode, hover-lägen, states och GUI-element.

### Kärnfärger

**Primära färger**

Scruffy Steel — `#2A2F36`  
(mörkt tekniskt gråblått, professionellt)

Architect Blue — `#3B80F7`  
(klar men inte skrikig engineering-blå)

**Sekundära färger**

Curiosity Amber — `#F4A623`  
(accent som signalerar nyfikenhet och aha-känsla)

Notebook White — `#F9F9F9`  
(clean bakgrund som känns som skisspapper)

**Bakgrundstoner**

Warm Grey — `#E4E4E4`

Blueprint Navy — `#1E2A38`

### Utökade UI-färger

**Light mode**
- Surface White — `#FFFFFF`
- Sub Surface — `#F4F6FA`
- Tag Bar — `#EEEFF2`
- Primary Hover — `#2A6EE8`
- Danger — `#E53935`
- Danger Hover — `#EF5350`
- Disabled — `#AAAAAA`
- Cancel — `#757575`
- Foreground Secondary — `#607884`
- Foreground Subtle — `#9AABB8`
- Border — `#D0D5DD`
- Selected Row Background — `#D6E6FF`
- Selected Row Foreground — `#1E2A38`
- Hover Row Background — `#EDF4FF`

**Dark mode**
- App Background — `#1E2A38`
- Surface Background — `#2A2F36`
- Sub Surface — `#232B37`
- Header Background — `#141C26`
- Sub Header Background — `#212C3A`
- Tag Bar Background — `#1A2432`
- Primary Action — `#2D65B0`
- Primary Hover — `#3574C4`
- Danger — `#E53935`
- Danger Hover — `#EF5350`
- Disabled / Cancel — `#4A5568`
- Foreground Primary — `#F9F9F9`
- Foreground Secondary — `#8B9DB5`
- Foreground Subtle — `#6A7E94`
- Border — `#3B4A5C`
- Search Surface — `#1A2330`
- Selected Row Background — `#2A4A7F`
- Selected Row Foreground — `#F9F9F9`
- Hover Row Background — `#2D3D52`
- Tree Node Foreground — `#D0D8E4`
- Tree Count Foreground — `#6A7E94`

### Normal mode och dark mode

**Normal / Light mode**
- Basen är ljus och ren, med `Notebook White` och vit surface som huvudytor.
- `Scruffy Steel` används i header och footer för att ge struktur.
- `Architect Blue` används för primära knappar, labels och tydliga handlingar.
- `Curiosity Amber` används som accent i logo, theme toggle och markerade fokusvärden.
- Hover och urval visas med ljusa blå toner för att hålla uttrycket luftigt och tekniskt.

**Dark mode**
- Basen skiftar till `Blueprint Navy` och `Scruffy Steel` för en djupare, mer fokuserad känsla.
- Primära handlingar använder en dämpad blå: `#2D65B0`, så att UI:t inte blir för hårt mot mörka ytor.
- Text är huvudsakligen `Notebook White` eller blågrå sekundärtoner.
- Borders och sekundära ytor används aktivt för separation eftersom mörka vyer annars lätt flyter ihop.
- `Curiosity Amber` behålls som tydlig brandaccent även i dark mode.

### Hur färgerna används i olika GUI-element

**App shell**
- Appbakgrund: `Notebook White` i light mode, `Blueprint Navy` i dark mode
- Primära paneler och kort: vit/light surface i light mode, `Scruffy Steel` i dark mode
- Header och footer: mörk strukturyta i båda lägen, men djupare i dark mode

**Branding**
- `Url` i logotypen använder `Curiosity Amber`
- `Vault` använder ljus text på mörk header

**Knappar**
- Primära actions: `Architect Blue` i light mode, `#2D65B0` i dark mode
- Hover: `#2A6EE8` i light mode, `#3574C4` i dark mode
- Delete/destruktiva actions: `#E53935`
- Hover för destruktiva actions: `#EF5350`
- Cancel: `#757575` i light mode, `#4A5568` i dark mode
- Disabled: `#AAAAAA` i light mode, `#4A5568` i dark mode

**Textfält och formulär**
- Form labels: `Architect Blue`
- Inputytor: vit/surface i light mode, mörk surface i dark mode
- Border: `#D0D5DD` i light mode, `#3B4A5C` i dark mode
- Text: `Scruffy Steel` i light mode, `Notebook White` i dark mode
- Searchfält använder egen surface för att tydligt ligga ovanpå sekundär bakgrund

**Dropdowns och ComboBox**
- Samma grundfärger som inputs
- Hoverade alternativ använder list-hover-färg
- Valda alternativ använder selected-row-färger
- Pilikon och sekundära detaljer använder secondary foreground

**Listor och tabeller**
- Rader använder surface background
- Hover på rad använder `#EDF4FF` i light mode och `#2D3D52` i dark mode
- Vald rad använder `#D6E6FF` i light mode och `#2A4A7F` i dark mode
- Text i vald rad använder mörk text i light mode och ljus text i dark mode
- Kolumnheaders använder sekundär yta snarare än primary accent

**TreeView / kategorinavigering**
- Vanliga noder använder neutral textfärg
- Gruppnoder använder sin kategorifärg
- Räknare och metadata använder subtle tone
- Vald nod får markerad bakgrund samt accentlinje i `Curiosity Amber`

**Taggar och filter**
- Filterrubriker använder `Architect Blue`
- Omarkerade filter använder vanlig textfärg
- Markerade filter använder `Curiosity Amber`

**Dialoger**
- Följer samma tema som resten av appen
- OK/Spara använder primary action-färg
- Cancel använder cancel-färgen
- Bakgrund och inputytor följer aktivt valt mode

### Kategorifärger i appen

Utöver brandpaletten finns en funktionell färgsättning för kategorigrupper:
- Work — `#3F51B5`
- Personal — `#00897B`
- Dev — `#6D4C41`
- Infra — `#455A64`

Dessa används i kategoriträd och category chips och fungerar som informationsfärger, inte som huvudfärger för brandet.

## 🔤 3. Typsnitt
**Rubriker**
Inter eller Montserrat (Google Fonts)
– modern, clean, teknisk men personlig.

**Brödtext**
Inter, Roboto, eller Source Sans Pro
– maximal läsbarhet, bra för dokumentation.

Monospace (kod / tekniska exempel)
- JetBrains Mono eller Fira Code

## 🔧 4. Logo Directions

Tre logostilar som passar din profil:

1. Minimal Tech + Personlig twist
- Silhuett av rufsig hårprofil
- Kombinerad med en enkel kugge eller krets-slinga
- Text: The Scruffy Engineer
- Undertext: Curious by Design

2. Sketchy Maker Style
- Handritad ikon: spretigt hår + skiftnyckel + penna
- Lite organisk, som ritad på ett ingenjörsblock
- Färger: Architect Blue + Curiosity Amber

3. Clean Architectural Mark
- Geometriska linjer som antyder “hårfrisyr + systemdiagram”
- Passar bra som favicon, GitHub-avatar och YouTube-profil
- Monokrom version för presentationsslides

🎤 5. Röst & tonalitet
Så här bör brandets röst kännas:
- Smart men avspänd
- Pragmatiskt teknisk
- Resonerande och pedagogisk
- Nyfiken, undersökande
- Gillar att förklara varför
- Öppen med process, iterationer och misslyckanden

Exempel på ton
“Jag ville förstå varför prestandan sjönk — så jag rev isär hela stacken, ritade om flödet och hittade tre flaskhalsar. Här är vad vi lärde oss.”

📝 6. Taglines och underrubriker
**Huvudtagline:**
- Curious by Design

**Alternativa taglines**
- Engineering the unexpected
- Because systems deserve curiosity
- Breaking things. Understanding why. Fixing better.
- Designed to explore
- From chaos to clarity

## 🧱 7. Bildspråk
Stil
- Varm, autentisk engineer-miljö
- Rufsigt hår, pennor, whiteboards, skisser
- Servers, diagram, notebook-pages, små post-its
- Naturligt ljus
- Inte försäljigt eller överproducerat

Vad som ska undvikas
- Stockfoton där någon sitter i hoodie vid laptop
- För “corporate bland” look
- För mycket sci-fi/neon

## 💻 8. Social Media / Web Profiles
**Intro (kort)**
Engineer, architect and curious problem-solver. I build systems, break patterns, and explore tech with a scruffy mind and sharp tools.

**About (lång)**
I’m an engineer and IT architect with a habit of pulling things apart to understand how they actually work. I design systems, build prototypes, experiment with infrastructure, and share my discoveries along the way.
My style is a mix of structured engineering and scruffy curiosity — part architect, part tinkerer. Here you'll find deep dives, experiments, diagrams, reflections, and the occasional chaotic sketch that unexpectedly leads to clarity.

## 📦 9. Förslag på domäner


- scruffyengineer.com
- scruffyengineer.dev
- scruffy.engineer
- thescruffyengineer.com
- curiousbydesign.dev
- scruffylabs.dev

Vill du att jag kontrollerar vilka som är lediga just nu?
