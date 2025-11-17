import {createApi, fetchBaseQuery} from '@reduxjs/toolkit/query/react';

const addParameter = (url, paramName, paramValue) => {
    if (!paramValue) {
        return url;
    }
    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}${paramName}=${paramValue}`;
};

String.prototype.addParameter = function (paramName, paramValue) {
    return addParameter(this, paramName, paramValue);
}

export const api = createApi({
    reducerPath: 'api',
    baseQuery: fetchBaseQuery({baseUrl: 'http://localhost:5000/api'}),
    endpoints: (builder) => ({
        getAvailableDatasets: builder.query({
            query: () => ({
                url: '/FileSystem/datasets',
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.datasetPaths;
            }
        }),
        uploadDatasetToServer: builder.mutation({
            query: (input) => ({
                url: '/FileSystem',
                method: 'POST',
                body: input,
                formData: true
            })
        }),
        registerDataset: builder.mutation({
            query: (datasetName) => ({
                url: '/TestRun',
                method: 'POST',
                body: {datasetName}
            })
        }),
        analyzeImages: builder.mutation({
            query: (testRunId) => ({
                url: '/Image',
                method: 'POST',
                body: {testRunId}
            })
        }),
        calculateClusters: builder.mutation({
            query: (testRunId) => ({
                url: '/Cluster',
                method: 'POST',
                body: {testRunId}
            })
        }),
        calculateMetrics: builder.mutation({
            query: (testRunId) => ({
                url: '/Metric',
                method: 'POST',
                body: {testRunId}
            })
        }),
        compareDatasets: builder.mutation({
            query: (testRunId) => ({
                url: '/Comparison',
                method: 'POST',
                body: {testRunId}
            })
        }),
        getTestRuns: builder.query({
            query: () => ({
                url: '/TestRun',
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.testRuns;
            }
        }),
        getAvailableImages: builder.query({
            query: ({testRunId, firstImageId}) => ({
                url: `/Image?TestRunId=${testRunId}`.addParameter('ImageId', firstImageId),
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.images;
            }
        }),
        getAvailableClusters: builder.query({
            query: (imageId) => ({
                url: `/Cluster?ImageId=${imageId}`,
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.clusters;
            }
        }),
        getImageMinutiae: builder.query({
            query: (imageId) => ({
                url: `/Minutiae/all?ImageId=${imageId}`,
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.minutiae;
            }
        }),
        getClusterMinutiae: builder.query({
            query: (clusterId) => ({
                url: `/Minutiae/cluster?ClusterId=${clusterId}`,
                method: 'GET'
            })
        }),
        getComparisonAggregate: builder.query({
            query: ({firstClusterId, secondClusterId, firstMinutiaId, secondMinutiaId}) => ({
                url: `/Comparison/aggregate`
                    .addParameter('FirstClusterId', firstClusterId)
                    .addParameter('SecondClusterId', secondClusterId)
                    .addParameter('FirstMinutiaId', firstMinutiaId)
                    .addParameter('SecondMinutiaId', secondMinutiaId),
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.comparisons;
            }
        }),
        getComparison: builder.query({
            query: ({firstClusterId, secondClusterId, firstMinutiaId, secondMinutiaId}) => ({
                url: `/Comparison/comparison`
                    .addParameter('FirstClusterId', firstClusterId)
                    .addParameter('SecondClusterId', secondClusterId)
                    .addParameter('FirstMinutiaId', firstMinutiaId)
                    .addParameter('SecondMinutiaId', secondMinutiaId),
                method: 'GET'
            }),
            transformResponse(baseQueryReturnValue) {
                return baseQueryReturnValue.dotDetails;
            }
        }),
    })
});

export const {
    useGetAvailableDatasetsQuery,
    useUploadDatasetToServerMutation,
    useRegisterDatasetMutation,
    useAnalyzeImagesMutation,
    useCalculateClustersMutation,
    useCalculateMetricsMutation,
    useCompareDatasetsMutation,
    useGetTestRunsQuery,
    useGetAvailableImagesQuery,
    useGetAvailableClustersQuery,
    useGetImageMinutiaeQuery,
    useGetClusterMinutiaeQuery,
    useGetComparisonAggregateQuery,
    useGetComparisonQuery
} = api;