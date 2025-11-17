import {configureStore} from "@reduxjs/toolkit";
import {api} from "./api.js";
import {setupListeners} from "@reduxjs/toolkit/query";
import testRunReducer from "./testRunSlice.js";
import analyticsReducer from "./analyticsSlice.js";

export const appStore = configureStore({
    reducer: {
        [api.reducerPath]: api.reducer,
        testRun: testRunReducer,
        analytics: analyticsReducer
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(api.middleware)
});

setupListeners(appStore.dispatch);
