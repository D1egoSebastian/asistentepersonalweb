// Pure rules for the tag field. They only improve what the owner sees while typing;
// the API applies the final rules when the idea is saved.

export const MAX_TAGS_PER_IDEA = 10;
const MAX_SUGGESTIONS = 8;

// Same rule as the backend: trim the ends and drop one leading "#".
export function normalizeTag(text) {
  return (text ?? "").trim().replace(/^#/, "").trim();
}

// "libros, cine" becomes ["libros", "cine"]; empty parts are dropped.
export function splitTags(text) {
  return (text ?? "")
    .split(",")
    .map(normalizeTag)
    .filter((tag) => tag !== "");
}

const sameTag = (a, b) => a.toLowerCase() === b.toLowerCase();

// Existing tags that start with the typed text, without the ones already chosen.
export function suggestTags(allTags, prefix, chosen = []) {
  const start = normalizeTag(prefix).toLowerCase();
  if (start === "") {
    return [];
  }

  const suggestions = [];
  for (const tag of allTags) {
    const alreadyListed = suggestions.some((listed) => sameTag(listed, tag));
    const alreadyChosen = chosen.some((picked) => sameTag(picked, tag));
    if (tag.toLowerCase().startsWith(start) && !alreadyListed && !alreadyChosen) {
      suggestions.push(tag);
    }
    if (suggestions.length === MAX_SUGGESTIONS) {
      break;
    }
  }
  return suggestions;
}

// Returns a new list. A blank tag, a repeated one, or a full idea leaves the list as it was.
export function addTag(chosen, tag, allTags = []) {
  const text = normalizeTag(tag);
  if (text === "" || chosen.length >= MAX_TAGS_PER_IDEA || chosen.some((picked) => sameTag(picked, text))) {
    return chosen;
  }

  const spelling = allTags.find((existing) => sameTag(existing, text)) ?? text;
  return [...chosen, spelling];
}

// Tags with at least one idea in the section being shown (active or archived), sorted for the filter.
export function filterOptions(tagCounts, archived) {
  return tagCounts
    .filter((tag) => (archived ? tag.archivedCount : tag.activeCount) > 0)
    .map((tag) => tag.name)
    .sort((a, b) => a.localeCompare(b, "es"));
}

// A hashtag starts a word: "#libros" counts, "C#" or "issue#4" do not.
const HASHTAG = /(^|\s)#([^\s#,]+)/g;

// Quick capture: "leer más #libros" saves the text "leer más" with the tag "libros".
// When the idea is only hashtags, their words stay as the text so it is never empty.
export function extractHashtags(input) {
  const words = [...input.matchAll(HASHTAG)].map((match) => match[2]);
  if (words.length === 0) {
    return { text: input, tags: [] };
  }

  const tags = words.reduce((chosen, word) => addTag(chosen, word), []);
  const text = input
    .replace(HASHTAG, "$1")
    .split("\n")
    .map((line) => line.replace(/[ \t]{2,}/g, " ").trim())
    .join("\n")
    .trim();
  return { text: text === "" ? words.join(" ") : text, tags };
}
