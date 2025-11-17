import {useDispatch, useSelector} from "react-redux";
import {
    useGetAvailableClustersQuery,
    useGetAvailableImagesQuery,
    useGetClusterMinutiaeQuery,
    useGetComparisonAggregateQuery,
    useGetComparisonQuery,
    useGetImageMinutiaeQuery
} from "../../redux/api.js";
import DisplayImage from "../displayImage/displayImage.jsx";
import {
    setFirstCluster,
    setFirstImage,
    setFirstMinutiaId,
    setSecondCluster,
    setSecondImage,
    setSecondMinutiaId
} from "../../redux/analyticsSlice.js";
import {useEffect, useState} from "react";
import {
    Box,
    Collapse,
    IconButton, Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Typography
} from "@mui/material";
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';

const AnalyticsComponent = () => {
    const dispatch = useDispatch();
    const {testRun} = useSelector((state) => state.testRun);
    const {
        firstImage,
        secondImage,
        firstCluster,
        secondCluster,
        firstMinutiaId,
        secondMinutiaId
    } = useSelector((state) => state.analytics);

    const getBackgroundImage = (imageId, backgroundType) => {
        if (!imageId) return;
        return fetch(`http://localhost:5000/api/FileSystem/image?ImageId=${imageId}`.addParameter('BackgroundType', backgroundType))
            .then(response => response.arrayBuffer().then(blob => {
                return window.URL.createObjectURL(new Blob([blob]));
            }));
    }

    const [firstBackgroundUrl, setFirstBackgroundUrl] = useState(null);
    const [firstBackgroundType, setFirstBackgroundType] = useState('Original');
    const [secondBackgroundUrl, setSecondBackgroundUrl] = useState(null);
    const [secondBackgroundType, setSecondBackgroundType] = useState('Original');

    const getImageWidth = (selectedBackgroundType, image) => {
        if (selectedBackgroundType === 'Original') {
            return 374;
        }
        return image.widthOffset - image.widthShift;
    }

    const getImageHeight = (selectedBackgroundType, image) => {
        if (selectedBackgroundType === 'Original') {
            return 388;
        }
        return image.heightOffset - image.heightShift;
    }

    useEffect(() => {
        if (firstImage) {
            getBackgroundImage(firstImage.id, 'Original').then(url => setFirstBackgroundUrl(url));
        }
        if (secondImage) {
            getBackgroundImage(secondImage.id, 'Original').then(url => setSecondBackgroundUrl(url));
        }
    }, [firstImage, secondImage]);

    const {data: firstImages} = useGetAvailableImagesQuery({
        testRunId: testRun?.id,
        firstImageId: null
    }, {skip: !testRun});

    const {data: secondImages} = useGetAvailableImagesQuery({
        testRunId: testRun?.id,
        firstImageId: firstImage?.id
    }, {skip: !testRun || !firstImage});

    const {data: firstImageMinutiae} = useGetImageMinutiaeQuery(firstImage?.id, {
        skip: !firstImage
    });

    const {data: secondImageMinutiae} = useGetImageMinutiaeQuery(secondImage?.id, {
        skip: !secondImage
    });

    const {data: firstClusters} = useGetAvailableClustersQuery(firstImage?.id, {
        skip: !firstImage
    });

    const {data: secondClusters} = useGetAvailableClustersQuery(secondImage?.id, {
        skip: !secondImage
    });

    const {data: firstClusterMinutiae} = useGetClusterMinutiaeQuery(firstCluster?.id, {
        skip: !firstCluster
    });

    const {data: secondClusterMinutiae} = useGetClusterMinutiaeQuery(secondCluster?.id, {
        skip: !secondCluster
    });

    const {data: comparisonAggregate} = useGetComparisonAggregateQuery({
        firstClusterId: firstCluster?.id,
        secondClusterId: secondCluster?.id,
        firstMinutiaId: firstMinutiaId,
        secondMinutiaId: secondMinutiaId
    }, {
        skip: !firstCluster || !secondCluster || !firstMinutiaId || !secondMinutiaId
    });

    const {data: comparison} = useGetComparisonQuery({
        firstClusterId: firstCluster?.id,
        secondClusterId: secondCluster?.id,
        firstMinutiaId: firstMinutiaId,
        secondMinutiaId: secondMinutiaId
    }, {
        skip: !firstCluster || !secondCluster || !firstMinutiaId || !secondMinutiaId
    });

    const onImageSelected = (imageId, images, action) => {
        const image = images.find(image => image.id === imageId);
        dispatch(action(image));
    }

    const onClusterSelected = (clusterId, clusters, action) => {
        const cluster = clusters.find(cluster => cluster.id === clusterId);
        dispatch(action(cluster));
    }

    const onBackgroundTypeSelected = (type, backgroundSelector, urlSelector, image) => {
        backgroundSelector(type);
        if (image) {
            getBackgroundImage(image.id, type).then(url => urlSelector(url));
        }
    }

    const onMinutiaSelected = (minutiaId, action) => {
        dispatch(action(minutiaId));
    }


    const CollapsibleRow = ({row}) => {
        const [open, setOpen] = useState(false);

        return (
            <>
                <TableRow sx={{ '& > *': { borderBottom: 'unset' } }}>
                    <TableCell>
                        <IconButton
                            aria-label="expand row"
                            size="small"
                            onClick={() => setOpen(!open)}
                        >
                            {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
                        </IconButton>
                    </TableCell>
                    <TableCell component="th" scope="row">
                        {row.firstMinutiaId}
                    </TableCell>
                    <TableCell align="right">{row.secondMinutiaId}</TableCell>
                    <TableCell align="right">
                        <IconButton
                            aria-label="expand row"
                            size="small"
                        >
                            {row.isMatch
                                ? <CheckCircleIcon color={'primary'}/>
                                : row.distanceDetails.status
                                    ? <>
                                        <CheckCircleIcon color={'primary'}/>
                                        <CancelIcon color={'error'} />
                                    </>
                                    : <>
                                        <CancelIcon color={'error'} />
                                        <CancelIcon color={'error'} />
                                    </>
                            }
                        </IconButton>
                    </TableCell>
                </TableRow>
                <TableRow>
                    <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
                        <Collapse in={open} timeout="auto" unmountOnExit>
                            <Box sx={{ margin: 1 }}>
                                <Typography variant="h6" gutterBottom component="div">
                                    Details
                                </Typography>
                                <Table size="small" aria-label="purchases">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Metric name</TableCell>
                                            <TableCell>Measurement unit</TableCell>
                                            <TableCell>Difference</TableCell>
                                            <TableCell align="right">Accepted threshold</TableCell>
                                            <TableCell align="right">Is passing</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        <TableRow>
                                            <TableCell component="th" scope="row">
                                                Distance
                                            </TableCell>
                                            <TableCell>millimeters</TableCell>
                                            <TableCell>{row.distanceDetails.value}</TableCell>
                                            <TableCell align="right">{row.distanceDetails.acceptableThreshold}</TableCell>
                                            <TableCell align="right">
                                                <IconButton
                                                    aria-label="expand row"
                                                    size="small"
                                                >
                                                    {row.distanceDetails.status ? <CheckCircleIcon color={'primary'}/> : <CancelIcon color={'error'} />}
                                                </IconButton>
                                            </TableCell>
                                        </TableRow>
                                        {
                                            !row.angleDetails
                                                ? null
                                                : <TableRow>
                                                    <TableCell component="th" scope="row">
                                                        Angle
                                                    </TableCell>
                                                    <TableCell>radians</TableCell>
                                                    <TableCell>{row.angleDetails.value}</TableCell>
                                                    <TableCell align="right">{row.angleDetails.acceptableThreshold}</TableCell>
                                                    <TableCell align="right">
                                                        <IconButton
                                                            aria-label="expand row"
                                                            size="small"
                                                        >
                                                            {row.angleDetails.status ? <CheckCircleIcon color={'primary'}/> : <CancelIcon color={'error'} />}
                                                        </IconButton>
                                                    </TableCell>
                                                </TableRow>
                                        }
                                    </TableBody>
                                </Table>
                            </Box>
                        </Collapse>
                    </TableCell>
                </TableRow>
            </>
        );
    }

    return (
        <>
            <div style={{
                display: 'flex',
                flexFlow: 'row',
                columnGap: '10px',
                width: '100%',
                marginTop: '10px',
                marginLeft: '10px',
                marginRight: '10px'
            }}>
                <DisplayImage
                    onClusterSelected={(clusterId) => onClusterSelected(clusterId, firstClusters, setFirstCluster)}
                    onImageSelected={(imageId) => onImageSelected(imageId, firstImages, setFirstImage)}
                    availableImages={firstImages}
                    availableClusters={firstClusters}
                    selectedImage={firstImage}
                    cluster={firstCluster}
                    imageMinutiae={firstImageMinutiae}
                    backgroundUrl={firstBackgroundUrl}
                    backgroundType={firstBackgroundType}
                    imageWidth={getImageWidth(firstBackgroundType, firstImage)}
                    imageHeight={getImageHeight(firstBackgroundType, firstImage)}
                    setBackgroundType={(type) => onBackgroundTypeSelected(type, setFirstBackgroundType, setFirstBackgroundUrl, firstImage)}
                    clusterDetails={firstClusterMinutiae}
                    onMinutiaSelected={(minutiaId) => onMinutiaSelected(minutiaId, setFirstMinutiaId)}
                    selectedMinutiaId={firstMinutiaId}
                    comparisons={comparison}
                />

                <DisplayImage
                    onClusterSelected={(clusterId) => onClusterSelected(clusterId, secondClusters, setSecondCluster)}
                    onImageSelected={(imageId) => onImageSelected(imageId, secondImages, setSecondImage)}
                    availableImages={secondImages}
                    availableClusters={secondClusters}
                    selectedImage={secondImage}
                    cluster={secondCluster}
                    imageMinutiae={secondImageMinutiae}
                    backgroundUrl={secondBackgroundUrl}
                    backgroundType={secondBackgroundType}
                    imageWidth={getImageWidth(secondBackgroundType, secondImage)}
                    imageHeight={getImageHeight(secondBackgroundType, secondImage)}
                    setBackgroundType={(type) => onBackgroundTypeSelected(type, setSecondBackgroundType, setSecondBackgroundUrl, secondImage)}
                    clusterDetails={secondClusterMinutiae}
                    onMinutiaSelected={(minutiaId) => onMinutiaSelected(minutiaId, setSecondMinutiaId)}
                    selectedMinutiaId={secondMinutiaId}
                    comparisons={comparison}
                />
            </div>
            {
                !comparisonAggregate
                    ? null
                    : <TableContainer component={Paper}>
                        <Table aria-label="collapsible table">
                            <TableHead>
                                <TableRow>
                                    <TableCell />
                                    <TableCell>First minutia id</TableCell>
                                    <TableCell align="right">Second minutia id</TableCell>
                                    <TableCell align="right">Is matching</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {comparisonAggregate.map((row, index) => (
                                    <CollapsibleRow key={index} row={row} />
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
            }
        </>
    );
};

export default AnalyticsComponent;