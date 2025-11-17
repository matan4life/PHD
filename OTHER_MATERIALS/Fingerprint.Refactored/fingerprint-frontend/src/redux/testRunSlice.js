import {createSlice} from "@reduxjs/toolkit";

const testRunSlice = createSlice({
    name: 'testRun',
    initialState: {
        testRun: null,
    },
    reducers: {
        setTestRun: (state, action) => {
            state.testRun = action.payload;
        },
    },
});

const { actions, reducer } = testRunSlice;
export const { setTestRun } = actions;
export default reducer;