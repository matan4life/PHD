import {createSlice} from "@reduxjs/toolkit";

const initialState = {
    firstImage: null,
    secondImage: null,
    firstCluster: null,
    secondCluster: null,
    firstMinutiaId: null,
    secondMinutiaId: null
};

const analyticsSlice = createSlice({
    name: 'analytics',
    initialState,
    reducers: {
        setFirstImage: (state, action) => {
            state.firstImage = action.payload;
        },
        setSecondImage: (state, action) => {
            state.secondImage = action.payload;
        },
        setFirstCluster: (state, action) => {
            state.firstCluster = action.payload;
        },
        setSecondCluster: (state, action) => {
            state.secondCluster = action.payload;
        },
        setFirstMinutiaId: (state, action) => {
            state.firstMinutiaId = action.payload;
        },
        setSecondMinutiaId: (state, action) => {
            state.secondMinutiaId = action.payload;
        }
    }
});

export const {
    setFirstImage,
    setSecondImage,
    setFirstCluster,
    setSecondCluster,
    setFirstMinutiaId,
    setSecondMinutiaId
} = analyticsSlice.actions;

export default analyticsSlice.reducer;