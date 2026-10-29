import { request } from "./http.js";

// Calls to the ideas API. Paths are relative: in development, Vite forwards /api to the ASP.NET Core server.
const BASE_PATH = "/api/ideas";

const ideaRequest = (method, path, body) => request(method, `${BASE_PATH}${path}`, body);

// Only the filters with a value are sent; the API treats a missing search or tag as "no filter".
export const listIdeas = ({ archived = false, search = "", tag = "" } = {}) => {
  const params = new URLSearchParams({ archived: String(archived) });
  if (search.trim() !== "") {
    params.set("search", search);
  }
  if (tag !== "") {
    params.set("tag", tag);
  }
  return ideaRequest("GET", `?${params}`);
};

export const getIdea = (id) => ideaRequest("GET", `/${id}`);

export const createIdea = (data) => ideaRequest("POST", "", data);

// The version is the one the owner opened; the API rejects the edit with 409 if the idea changed since.
export const updateIdea = (id, data) => ideaRequest("PUT", `/${id}`, data);

export const archiveIdea = (id) => ideaRequest("POST", `/${id}/archive`);

export const restoreIdea = (id) => ideaRequest("POST", `/${id}/restore`);

export const deleteIdea = (id) => ideaRequest("DELETE", `/${id}`);

export const listTags = () => ideaRequest("GET", "/tags");
