import {FormControl, InputLabel, MenuItem, Select} from "@mui/material";
import PropTypes from "prop-types";
import {
    CartesianGrid,
    Cell,
    LabelList,
    ResponsiveContainer,
    Scatter,
    ScatterChart,
    Tooltip,
    XAxis,
    YAxis
} from "recharts";

const DisplayImage = ({
                          onImageSelected,
                          selectedImage,
                          availableImages,
                          cluster,
                          onClusterSelected,
                          availableClusters,
                          imageMinutiae,
                          backgroundUrl,
                          imageWidth,
                          backgroundType,
                          setBackgroundType,
                          imageHeight,
                          clusterDetails,
                          onMinutiaSelected,
                          selectedMinutiaId,
                          comparisons
                      }) => {

    if (!availableImages || availableImages.length === 0) {
        return null;
    }

    const getData = () => {
        return imageMinutiae.map(minutia => ({
            x: minutia.x,
            y: imageHeight - minutia.y,
            id: minutia.id
        }));
    }

    function hslToHex(h, s, l) {
        l /= 100;
        const a = s * Math.min(l, 1 - l) / 100;
        const f = n => {
            const k = (n + h / 30) % 12;
            const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
            return Math.round(255 * color).toString(16).padStart(2, '0');   // convert to Hex and prefix "0" if needed
        };
        return `#${f(0)}${f(8)}${f(4)}`;
    }

    // Create sequence from 0 to 360 for 18 elements
    const colors = Array.from({length: 19}, (_, i) => hslToHex(i * 19, 100, 50));

    const getColor = (entry) => {
        if (clusterDetails && clusterDetails.centroid.id === entry.id) {
            return colors[0];
        }

        if (entry.id === selectedMinutiaId){
            return colors[1];
        }

        if (comparisons) {
            const [c1, c2, c3, c4, ...rest] = colors;
            const firstDots = comparisons.map(c => c.firstMinutiaId);
            const secondDots = comparisons.map(c => c.secondMinutiaId);

            if (firstDots.includes(entry.id)) {
                return rest[firstDots.indexOf(entry.id)];
            }
            if (secondDots.includes(entry.id)) {
                return rest[secondDots.indexOf(entry.id)];
            }
        }

        if (clusterDetails && clusterDetails.minutiae.some(m => m.id === entry.id)) {
            return colors[2]
        }

        return colors[3];
    }

    const getIntro = (payload) => {
        const id = payload.payload.id;
        if (clusterDetails && cluster && clusterDetails.centroid.id === id) {
            return `Is centroid of cluster ${cluster.id}`;
        }
        if (clusterDetails && cluster && clusterDetails.minutiae.some(m => m.id === id)) {
            return `Is member of cluster ${cluster.id}`;
        }
        if (cluster) {
            return `Is not member of cluster ${cluster.id}`;
        }
    }

    const CustomTooltip = ({active, payload, label}) => {
        if (active && payload && payload.length) {
            return (
                <div className="custom-tooltip">
                    {payload.map(p =>
                        <>
                            <p className={'label'}>{`${p.name}: ${p.value}`}</p>
                        </>)}
                    <p className="intro">{getIntro(payload[0])}</p>
                    {comparisons && comparisons.map(c => c.firstMinutiaId).includes(payload[0].payload.id)
                        ? <p className="intro">Is successfully compared with
                            minutia {comparisons[comparisons.map(c => c.firstMinutiaId).indexOf(payload[0].payload.id)].secondMinutiaId}</p>
                        : null}
                    {comparisons && comparisons.map(c => c.secondMinutiaId).includes(payload[0].payload.id)
                        ? <p className="intro">Is successfully compared with
                            minutia {comparisons[comparisons.map(c => c.secondMinutiaId).indexOf(payload[0].payload.id)].firstMinutiaId}</p>
                        : null}
                </div>
            );
        }

        return null;
    };

    return (
        <div style={{
            display: 'flex',
            flexFlow: 'column',
            rowGap: '10px',
            width: '40%',
            flexGrow: 15,
            alignItems: "center"
        }}>
            <FormControl fullWidth>
                <InputLabel id="image">Image</InputLabel>
                <Select
                    labelId="image"
                    id="image-select"
                    value={selectedImage?.id ?? 0}
                    label="Image"
                    onChange={(e) => onImageSelected(e.target.value)}
                    variant={'filled'}>
                    {availableImages.map((image) => (
                        <MenuItem key={image.id} value={image.id}>{image.fileName}</MenuItem>))}
                </Select>
            </FormControl>
            {
                !availableClusters || availableClusters.length === 0
                    ? null
                    :
                    <>
                        <FormControl fullWidth>
                            <InputLabel id="cluster">Cluster</InputLabel>
                            <Select
                                labelId="cluster"
                                id="cluster-select"
                                value={cluster?.id ?? 0}
                                label="Dataset name"
                                onChange={(e) => onClusterSelected(e.target.value)}
                                variant={'filled'}>
                                {availableClusters.map((cluster) => (
                                    <MenuItem key={cluster.id} value={cluster.id}>{cluster.id}</MenuItem>))}
                            </Select>
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel id="background">Background</InputLabel>
                            <Select
                                labelId="background"
                                id="background-select"
                                value={backgroundType}
                                label="Background type"
                                onChange={(e) => setBackgroundType(e.target.value)}
                                variant={'filled'}>
                                <MenuItem value={'Original'}>Original</MenuItem>
                                <MenuItem value={'Enhanced'}>Enhanced</MenuItem>
                                <MenuItem value={'Skeleton'}>Skeleton</MenuItem>
                            </Select>
                        </FormControl>
                    </>
            }
            {
                !cluster || !clusterDetails || clusterDetails.minutiae.length === 0
                    ? null
                    :
                    <>
                        <FormControl fullWidth>
                            <InputLabel id="leading-minutiae">Leading minutiae</InputLabel>
                            <Select
                                labelId="leading-minutiae"
                                id="leading-minutiae-select"
                                value={selectedMinutiaId}
                                label="Leading minutiae"
                                onChange={(e) => onMinutiaSelected(e.target.value)}
                                variant={'filled'}>
                                {clusterDetails.minutiae.map((minutia) => (
                                    <MenuItem key={minutia.id} value={minutia.id}>{minutia.id}</MenuItem>))}
                            </Select>
                        </FormControl>
                    </>
            }
            {
                !imageMinutiae || imageMinutiae.length === 0
                    ? null
                    : <>
                        <img src={backgroundUrl} alt={'Background'} style={{width: imageWidth, height: imageHeight}}/>
                        <ResponsiveContainer width={800} height={800}>
                            <ScatterChart
                                margin={{
                                    top: 20,
                                    right: 20,
                                    bottom: 20,
                                    left: 20,
                                }}
                            >
                                <CartesianGrid/>
                                <XAxis type="number" dataKey="x" name="X coordinate" unit=""/>
                                <YAxis type="number" dataKey="y" name="Y coordinate" unit=""/>
                                <Tooltip cursor={{strokeDasharray: '3 3'}}
                                         wrapperStyle={{backgroundColor: "rgba(255, 255, 255, 0.75)"}}
                                         content={<CustomTooltip/>}/>
                                <Scatter name="A school" data={getData()} fill="#8884d8">
                                    <LabelList dataKey={'id'} position="bottom"/>
                                    {
                                        getData().map((entry, index) => (
                                            <Cell key={`cell-${index}`} fill={getColor(entry)}/>
                                        ))
                                    }
                                </Scatter>
                            </ScatterChart>
                        </ResponsiveContainer>
                    </>
            }
        </div>
    )
};

const imageType = {
    id: PropTypes.number.isRequired,
    fileName: PropTypes.string.isRequired,
    widthShift: PropTypes.number.isRequired,
    heightShift: PropTypes.number.isRequired,
    widthOffset: PropTypes.number.isRequired,
    heightOffset: PropTypes.number.isRequired
};

const minutia = {
    id: PropTypes.number.isRequired,
    x: PropTypes.number.isRequired,
    y: PropTypes.number.isRequired,
    theta: PropTypes.number.isRequired
};

DisplayImage.propTypes = {
    onImageSelected: PropTypes.func.isRequired,
    selectedImage: PropTypes.shape(imageType),
    availableImages: PropTypes.arrayOf(PropTypes.shape(imageType)),
    cluster: PropTypes.shape({
        id: PropTypes.number.isRequired
    }),
    onClusterSelected: PropTypes.func.isRequired,
    availableClusters: PropTypes.arrayOf(PropTypes.number),
    imageMinutiae: PropTypes.arrayOf(PropTypes.shape(minutia)),
    backgroundUrl: PropTypes.string,
    imageWidth: PropTypes.number.isRequired,
    imageHeight: PropTypes.number.isRequired,
    backgroundType: PropTypes.string.isRequired,
    setBackgroundType: PropTypes.func.isRequired
};

export default DisplayImage;