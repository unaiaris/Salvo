import { DEFAULT_LANGUAGE, type Language } from "@/lib/api/contract";
import { es } from "./es";
import { pt } from "./pt";

/**
 * Every word this console renders, in the language of the deployment.
 *
 * <h3>Why the key set is a type and not a convention</h3>
 *
 * {@link Dictionary} is `typeof es`, so {@link pt} — which declares itself of that type — cannot
 * omit a key or invent one: both are compile errors. That is the one property of this module that
 * is not negotiable. A `t()` that fell back to the other language for a missing key would leave
 * half a screen untranslated with every test green, and a screen that looks finished and is not is
 * worse than one that was never started: it makes a promise the deployment does not keep.
 *
 * Spanish is the source of the key set because it is the default of the API, the language of the
 * demonstration, and the one a reviewer sees on a fresh checkout.
 *
 * <h3>Sentences, not words</h3>
 *
 * Values are whole sentences, and the ones that carry a figure are functions of that figure rather
 * than templates stitched together at the call site. Word order and agreement differ between the
 * two languages, so a sentence assembled from fragments by shared code is grammatical in neither.
 * It also means a translator reads a sentence and answers with a sentence.
 *
 * <h3>How it reaches a component</h3>
 *
 * The language is a primitive — `"es"` or `"pt"` — and it travels as one. Server components take it
 * from `capabilities` and pass it down; client components receive it as a string prop and look up
 * their own words here. Nothing hands a client component the dictionary itself, which would be an
 * object crossing the boundary, and nothing hands it a rendered label either: `E7B` found that a
 * label passed as a prop is serialised into the RSC payload of every page whether or not anything
 * renders it. Words that belong to a control live with the control.
 */
export type Dictionary = typeof es;

const DICTIONARIES: Readonly<Record<Language, Dictionary>> = { es, pt };

/** The dictionary of a language. Total by construction: `Language` has no other members. */
export function messagesFor(language: Language): Dictionary {
  return DICTIONARIES[language];
}

/**
 * The dictionary to compose with when the language could not be established.
 *
 * A failure to read the capabilities is a failure of presentation, not of the console: the page
 * still has to render, and it renders in the default. The notice about the unreadable capabilities
 * is on the screen either way.
 */
export const FALLBACK: Dictionary = DICTIONARIES[DEFAULT_LANGUAGE];
