import axios from "axios";

const url = "http://localhost:5000/api";
export const get = path => axios.get(`${url}/${path}`);
export const post = (path, body, config) => {
    return axios.post(`${url}/${path}`, body, config);
};