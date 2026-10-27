import { request } from "./http.js";

// Calls to the habits API. Paths are relative: in development, Vite forwards /api to the ASP.NET Core server.
const BASE_PATH = "/api/habits";

const habitRequest = (method, path, body) => request(method, `${BASE_PATH}${path}`, body);

export const listHabits = () => habitRequest("GET", "");

export const createHabit = (data) => habitRequest("POST", "", data);

export const updateHabit = (id, data) => habitRequest("PUT", `/${id}`, data);

export const deleteHabit = (id) => habitRequest("DELETE", `/${id}`);

export const logHabit = (id, date) => habitRequest("POST", `/${id}/log`, { date });

export const unlogHabit = (id, date) => habitRequest("DELETE", `/${id}/log/${date}`);
