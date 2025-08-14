using Api.Database;
using Api.Entities;
using MathNet.Numerics.Statistics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Api.Features.Comparisons.Queries.GetAllComparisons;

public sealed class GetAllComparisonsQueryHandler(FingerprintContext context) : IRequestHandler<GetAllComparisonsQuery, int>
{
    public async Task<int> Handle(GetAllComparisonsQuery request, CancellationToken cancellationToken)
    {
        EnsureFolder("reports");
        var images = await context.Images
            .Include(x => x.Clusters)
            .ThenInclude(x => x.ClusterMinutiaes)
            .ThenInclude(x => x.Minutia)
            .ToListAsync();
        var imageStats = new Dictionary<(string, string), int>();
        var document = ToDocument($"Image comparison report.", isA0: true, isLandscape: true);
        static double toAngleFrom0To2Pi(double angle) => (angle + 2 * Math.PI) % (2 * Math.PI);
        static double centroidOrdering(Minutia minutia, Minutia baseMinutia) => toAngleFrom0To2Pi(toAngleFrom0To2Pi(Math.Atan2(minutia.Y, minutia.X)) - toAngleFrom0To2Pi(Math.Atan2(baseMinutia.Y, baseMinutia.X)));
        static int toCorrectOrdinate(int y) => 388 - y;
        const double limit = 7.0;
        foreach (var image in images)
        {
            foreach (var anotherImage in images.Where(x => x.Id > image.Id))
            {
                var clusterInfo = new Dictionary<(int, int), int>();
                foreach (var cluster in image.Clusters)
                {
                    foreach (var anotherCluster in anotherImage.Clusters)
                    {
                        var rotations = new Dictionary<(int, int), int>();
                        var centroid1 = cluster.ClusterMinutiaes.First(x => x.IsCentroid).Minutia;
                        var minutiae1 = cluster.ClusterMinutiaes.Where(x => !x.IsCentroid).Select(x => new Minutia
                        {
                            Id = x.Minutia.Id,
                            X = x.Minutia.X - centroid1.X,
                            Y = toCorrectOrdinate(x.Minutia.Y) - toCorrectOrdinate(centroid1.Y),
                            IsTermination = x.Minutia.IsTermination
                        }).ToList();
                        var centroid2 = anotherCluster.ClusterMinutiaes.First(x => x.IsCentroid).Minutia;
                        var minutiae2 = anotherCluster.ClusterMinutiaes.Where(x => !x.IsCentroid).Select(x => new Minutia
                        {
                            Id = x.Minutia.Id,
                            X = x.Minutia.X - centroid2.X,
                            Y = toCorrectOrdinate(x.Minutia.Y) - toCorrectOrdinate(centroid2.Y),
                            IsTermination = x.Minutia.IsTermination
                        }).ToList();
                        foreach (var minutia1 in minutiae1)
                        {
                            var leftMinutia1 = minutiae1.Where(x => x.Id != minutia1.Id).OrderBy(x => centroidOrdering(x, minutia1)).ToList();
                            foreach (var minutia2 in minutiae2)
                            {
                                var leftMinutia2 = minutiae2.Where(x => x.Id != minutia2.Id).OrderBy(x => centroidOrdering(x, minutia2)).ToList();
                                var diff = leftMinutia1.Zip(leftMinutia2, (x, y) => (Math.Abs(GetDistance(x, minutia1) - GetDistance(y, minutia2)), x.IsTermination == y.IsTermination));
                                var countOfLessThanLimit = diff.Count(x => x.Item1 < limit && x.Item2);
                                rotations.Add((minutia1.Id, minutia2.Id), countOfLessThanLimit);
                            }
                        }
                        clusterInfo.Add((cluster.Id, anotherCluster.Id), rotations.Values.Max());
                    }
                }
                imageStats.Add((image.FileName, anotherImage.FileName), clusterInfo.Values.Max());
                imageStats.Add((anotherImage.FileName, image.FileName), clusterInfo.Values.Max());
            }
        }
        foreach (var image in images)
        {
            imageStats.Add((image.FileName, image.FileName), 0);
        }
        var imageData = images.OrderBy(x => x.FileName)
            .Select(x => new[] { x.FileName.Replace("_", @"\_") }
                .Concat(images.OrderBy(x => x.FileName).Select(y => imageStats[(x.FileName, y.FileName)].ToString())));
        var imageTableHeader = $@" & {string.Join(" & ", images.Select(x => x.FileName.Replace("_", @"\_")).Order())}";
        var imageTable = ToTable(imageData,
            $"Pairwise distance difference between images.",
            $"table:pairwise_diff_image",
            "tiny",
            customTableHeader: imageTableHeader,
            colorAnyCoincidense: true);
        document = document.WithGroup("document", new StringBuilder(imageTable));
        await File.WriteAllTextAsync($"reports/report.tex", document.ToString(), cancellationToken);
        foreach (var image in images)
        {
            foreach (var anotherImage in images.Where(x => x.Id > image.Id))
            {
                EnsureFolder($"reports/{Path.GetFileNameWithoutExtension(image.FileName)}_{Path.GetFileNameWithoutExtension(anotherImage.FileName)}");
                var clusterInfo = new Dictionary<(int, int), int>();
                document = ToDocument($"Image comparison report for fingerprint images {image.FileName.Replace("_", @"\_")} and {anotherImage.FileName.Replace("_", @"\_")}.");
                var imageAbstractLetter = new StringBuilder()
                    .WithRenderedTitle()
                    .WithGroup("abstract", new StringBuilder(AbstractLatexDocument()))
                    .WithLineBreak();
                var section1 = new StringBuilder().WithSection($"Image {image.FileName.Replace("_", @"\_")}");
                foreach (var cluster in image.Clusters)
                {
                    section1 = section1.WithSubsection($"Cluster {cluster.Id}")
                        .Append(ToMinutiaeReport(cluster))
                        .WithLineBreak()
                        .Append(@"\pagebreak")
                        .WithLineBreak();
                }
                var section2 = new StringBuilder().WithSection($"Image {anotherImage.FileName.Replace("_", @"\_")}");
                foreach (var cluster in anotherImage.Clusters)
                {
                    section2 = section2.WithSubsection($"Cluster {cluster.Id}")
                        .Append(ToMinutiaeReport(cluster))
                        .WithLineBreak()
                        .Append(@"\pagebreak")
                        .WithLineBreak();
                }
                foreach (var cluster in image.Clusters)
                {
                    foreach (var anotherCluster in anotherImage.Clusters)
                    {
                        var clusterCompDocument = ToDocument($"Cluster {cluster.Id} and {anotherCluster.Id}");
                        var clusterAbstract = new StringBuilder()
                            .WithRenderedTitle()
                            .WithGroup("abstract", new StringBuilder(AbstractLatexDocument()))
                            .WithLineBreak();
                        var section = new StringBuilder().WithSection($"Cluster comparison")
                            .WithLineBreak()
                            .AppendLine($@"\large \NB To prevent table duplication please refer to pairwise tables. \normalsize \\");
                        var subsections = new List<StringBuilder>();
                        var rotations = new Dictionary<(int, int), int>();
                        var centroid1 = cluster.ClusterMinutiaes.First(x => x.IsCentroid).Minutia;
                        var minutiae1 = cluster.ClusterMinutiaes.Where(x => !x.IsCentroid).Select(x => new Minutia
                        {
                            Id = x.Minutia.Id,
                            X = x.Minutia.X - centroid1.X,
                            Y = toCorrectOrdinate(x.Minutia.Y) - toCorrectOrdinate(centroid1.Y),
                            IsTermination = x.Minutia.IsTermination
                        }).ToList();
                        var centroid2 = anotherCluster.ClusterMinutiaes.First(x => x.IsCentroid).Minutia;
                        var minutiae2 = anotherCluster.ClusterMinutiaes.Where(x => !x.IsCentroid).Select(x => new Minutia
                        {
                            Id = x.Minutia.Id,
                            X = x.Minutia.X - centroid2.X,
                            Y = toCorrectOrdinate(x.Minutia.Y) - toCorrectOrdinate(centroid2.Y),
                            IsTermination = x.Minutia.IsTermination
                        }).ToList();
                        foreach (var minutia1 in minutiae1)
                        {
                            var leftMinutia1 = minutiae1.Where(x => x.Id != minutia1.Id).OrderBy(x => centroidOrdering(x, minutia1)).ToList();
                            foreach (var minutia2 in minutiae2)
                            {
                                var leftMinutia2 = minutiae2.Where(x => x.Id != minutia2.Id).OrderBy(x => centroidOrdering(x, minutia2)).ToList();
                                var diff = leftMinutia1.Zip(leftMinutia2, (x, y) => (Math.Abs(GetDistance(x, minutia1) - GetDistance(y, minutia2)), x.IsTermination == y.IsTermination));
                                var subsection = new StringBuilder().WithSubsection($"Minutiae {minutia1.Id} and {minutia2.Id}");
                                var firstDistance = new[] { "$d_{1}$" }.Concat(leftMinutia1.Select(x => GetDistance(x, minutia1).ToString("F2"))).ToList();
                                var secondDistance = new[] { "$d_{2}$" }.Concat(leftMinutia2.Select(x => GetDistance(x, minutia2).ToString("F2"))).ToList();
                                var distanceDiff = new[] { "$|d_{1}-d{2}|$" }.Concat(diff.Select(x => x.Item1.ToString("F2"))).ToList();
                                var firstTermination = new[] {"$t_{1}$"}.Concat(leftMinutia1.Select(x => x.IsTermination.ToString())).ToList();
                                var secondTermination = new[] { "$t_{2}$" }.Concat(leftMinutia2.Select(x => x.IsTermination.ToString())).ToList();
                                var terminationDiff = new[] { @"$t_{1} \odot t{2}$" }.Concat(diff.Select(x => x.Item2 ? "1" : "0")).ToList();
                                var summaryHeader = $@" & {string.Join(" & ", Enumerable.Range(1, leftMinutia1.Count))}";
                                var diffTable = ToTable($"Pairwise distances difference between {cluster.Id} (leading minutia {minutia1.Id}) and {anotherCluster.Id} (leading minutia {minutia2.Id}).",
                                    $"table:pairwise_diff_{minutia1.Id}_{minutia2.Id}",
                                    "centering",
                                    customTableHeader: summaryHeader,
                                    firstDistance,
                                    secondDistance,
                                    distanceDiff,
                                    firstTermination,
                                    secondTermination,
                                    terminationDiff);
                                var quantiles = new[]
                                {
                                    "$|d|_{0.1}$",
                                    "$|d|_{0.2}$",
                                    "$|d|_{0.25}$",
                                    "$|d|_{0.3}$",
                                    "$|d|_{0.4}$",
                                    "$|d|_{0.5}$",
                                    "$|d|_{0.6}$",
                                    "$|d|_{0.7}$",
                                    "$|d|_{0.75}$",
                                    "$|d|_{0.8}$",
                                    "$|d|_{0.9}$",
                                };
                                var quantileHeader = string.Join(" & ", quantiles);
                                var diff1 = diff.Select(x => x.Item1);
                                var data = new List<string>
                                {
                                    diff1.Quantile(0.1).ToString("F2"),
                                    diff1.Quantile(0.2).ToString("F2"),
                                    diff1.Quantile(0.25).ToString("F2"),
                                    diff1.Quantile(0.3).ToString("F2"),
                                    diff1.Quantile(0.4).ToString("F2"),
                                    diff1.Quantile(0.5).ToString("F2"),
                                    diff1.Quantile(0.6).ToString("F2"),
                                    diff1.Quantile(0.7).ToString("F2"),
                                    diff1.Quantile(0.75).ToString("F2"),
                                    diff1.Quantile(0.8).ToString("F2"),
                                    diff1.Quantile(0.9).ToString("F2")
                                };
                                var statTable = ToTable([data], $"Quantiles for pairwise distances difference between {cluster.Id} (leading minutia {minutia1.Id}) and {anotherCluster.Id} (leading minutia {minutia2.Id}).",
                                    $"table:pairwise_diff_quantiles_{minutia1.Id}_{minutia2.Id}",
                                    "centering",
                                    customTableHeader: quantileHeader);
                                var formula = ToStatisticsFormula(diff1, minutia1.Id, minutia2.Id, "minutiae");
                                subsection = subsection.AppendLine(diffTable)
                                    .AppendLine(statTable)
                                    .AppendLine(formula)
                                    .WithTag("pagebreak")
                                    .WithLineBreak();
                                subsections.Add(subsection);
                                var countOfLessThanLimit = diff.Count(x => x.Item1 < limit && x.Item2);
                                rotations.Add((minutia1.Id, minutia2.Id), countOfLessThanLimit);
                            }
                        }
                        var tableHeader = $@" & {string.Join(" & ", minutiae2.Select(x => x.Id).Order())}";
                        var clusterData = minutiae1.OrderBy(x => x.Id)
                            .Select(x => new[] { x.Id.ToString() }
                                .Concat(minutiae2.OrderBy(x => x.Id).Select(y => rotations[(x.Id, y.Id)].ToString())));
                        clusterInfo.Add((cluster.Id, anotherCluster.Id), rotations.Values.Max());
                        var bestRotation = rotations.OrderByDescending(x => x.Value).First();
                        var columnIndex = minutiae2.Select(x => x.Id).Order().ToList().IndexOf(bestRotation.Key.Item2) + 1;
                        var rowIndex = minutiae1.Select(x => x.Id).Order().ToList().IndexOf(bestRotation.Key.Item1);
                        var table = ToTable(clusterData,
                            $"Pairwise distance difference between cluster {cluster.Id} and {anotherCluster.Id}.",
                            $"table:pairwise_diff_cluster_{cluster.Id}_{anotherCluster.Id}",
                            "centering",
                            customTableHeader: tableHeader,
                            rowColoured: rowIndex,
                            columnColoured: columnIndex);
                        section = section.Append(table);
                        foreach (var subsubsection in subsections)
                        {
                            section = section.Append(subsubsection);
                        }
                        clusterAbstract = clusterAbstract.Append(section);
                        var finalClusterDocument = clusterCompDocument.WithGroup("document", clusterAbstract);
                        await File.WriteAllTextAsync($"reports/{Path.GetFileNameWithoutExtension(image.FileName)}_{Path.GetFileNameWithoutExtension(anotherImage.FileName)}/report_{cluster.Id}_{anotherCluster.Id}.tex", finalClusterDocument.ToString(), cancellationToken);
                    }
                }
                var imageData1 = image.Clusters.OrderBy(x => x.Id)
                    .Select(x => new[] { x.Id.ToString() }
                        .Concat(anotherImage.Clusters.OrderBy(x => x.Id).Select(y => clusterInfo[(x.Id, y.Id)].ToString())));
                var clusterBestRotation = clusterInfo.OrderByDescending(x => x.Value).First();
                var clusterColumnIndex = anotherImage.Clusters.Select(x => x.Id).Order().ToList().IndexOf(clusterBestRotation.Key.Item2) + 1;
                var clusterRowIndex = image.Clusters.Select(x => x.Id).Order().ToList().IndexOf(clusterBestRotation.Key.Item1);
                var clusterTableHeader = $@" & {string.Join(" & ", anotherImage.Clusters.Select(x => x.Id).Order())}";
                var imageTable1 = ToTable(imageData1,
                    $"Pairwise distance difference between image {image.FileName.Replace("_", @"\_")} and {anotherImage.FileName.Replace("_", @"\_")}.",
                    $"table:pairwise_diff_image_{image.Id}_{anotherImage.Id}",
                    "centering",
                    customTableHeader: clusterTableHeader,
                    rowColoured: clusterRowIndex,
                    columnColoured: clusterColumnIndex);
                imageAbstractLetter = imageAbstractLetter
                    .Append(section1)
                    .WithLineBreak()
                    .Append(section2)
                    .WithLineBreak()
                    .Append(imageTable1);
                var finalDocument = document.WithGroup("document", imageAbstractLetter);
                await File.WriteAllTextAsync($"reports/{Path.GetFileNameWithoutExtension(image.FileName)}_{Path.GetFileNameWithoutExtension(anotherImage.FileName)}/report_{image.FileName}_{anotherImage.FileName}.tex", finalDocument.ToString(), cancellationToken);
            }
        }
        return 0;
    }

    private static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static StringBuilder ToDocument(string document, bool isA0 = false, bool isLandscape = false)
    {
        var annotations = new[] { "letterpaper", "titlepage", "leqno", "draft" };
        if (isLandscape)
        {
            annotations = [.. annotations, "landscape"];
        }
        return StringBuilderExtensions.CreateLatexDocument()
            .WithDocumentAnnotations(annotations).WithCurlyBrace("article").WithLineBreak()
            .WithPackage("geometry", ["margin=0.1in", $"{(isA0 ? "a0" : "a4")}paper"])
            .WithPackage("amsmath")
            .WithPackage("amssymb")
            .WithPackage("scrextend")
            .WithPackage("datetime2")
            .WithPackage("titlesec")
            .WithPackage("float")
            .WithPackage("xcolor", ["table"])
            .WithPackage("caption", ["figurename=Formula"])
            .WithTag("counterwithin").WithCurlyBrace("table").WithCurlyBrace("subsection").WithLineBreak()
            .WithTitle([document])
            .WithAuthor(["Yurii Pohuliaiev"])
            .WithDate()
            .WithTitleFormat("part".ToTag().ToString(),
                "display",
                null,
                "filcenter".ToTag().WithTag("scshape").WithTag("huge").WithTag("partname").WithTag("enspace").WithTag("thepart").ToString(),
                "5pt",
                "filcenter".ToSpacedTag().WithSpacedTag("Huge").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat("section".ToTag().ToString(),
                "display",
                null,
                "filcenter".ToTag().WithTag("scshape").WithTag("Large").WithCurlyBrace("$Chapter$").WithTag("enspace").WithTag("thesection").ToString(),
                "5pt",
                "filcenter".ToSpacedTag().WithSpacedTag("LARGE").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat("subsection".ToTag().ToString(),
                "display",
                null,
                "filcenter".ToTag().WithTag("scshape").WithTag("large").WithCurlyBrace("$Section$").WithTag("enspace").WithTag("thesubsection").ToString(),
                "10pt",
                "filcenter".ToSpacedTag().WithSpacedTag("large").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat("subsubsection".ToTag().ToString(),
                "display",
                null,
                "filcenter".ToTag().WithTag("scshape").WithCurlyBrace("$Paragraph$").WithTag("enspace").WithTag("thesubsubsection").ToString(),
                "10pt",
                "filcenter".ToSpacedTag().WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat(new StringBuilder("name=").WithTag("part").Append(", numberless").ToString(),
                "block",
                null,
                null,
                "0pt",
                "filcenter".ToSpacedTag().WithSpacedTag("Huge").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat(new StringBuilder("name=").WithTag("section").Append(", numberless").ToString(),
                "block",
                null,
                null,
                "0pt",
                "filcenter".ToSpacedTag().WithSpacedTag("LARGE").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat(new StringBuilder("name=").WithTag("subsection").Append(", numberless").ToString(),
                "block",
                null,
                null,
                "0pt",
                "filcenter".ToSpacedTag().WithSpacedTag("large").WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleFormat(new StringBuilder("name=").WithTag("subsubsection").Append(", numberless").ToString(),
                "block",
                null,
                null,
                "0pt",
                "filcenter".ToSpacedTag().WithTag("bfseries").ToString(),
                "vskip4.5pt".ToTag().WithTag("titlerule").ToString())
            .WithTitleSpacing("part".ToTag().ToString(), "0pt", "-15pt", "25.5pt")
            .WithTitleSpacing(new StringBuilder("name=").WithTag("part").Append(", numberless").ToString(), "0pt", "16pt", "15pt")
            .WithTitleSpacing("section".ToTag().ToString(), "0pt", "-15pt", "25.5pt")
            .WithTitleSpacing(new StringBuilder("name=").WithTag("section").Append(", numberless").ToString(), "0pt", "16pt", "15pt")
            .WithTitleSpacing("subsection".ToTag().ToString(), "0pt", "-15pt", "25.5pt")
            .WithTitleSpacing(new StringBuilder("name=").WithTag("subsection").Append(", numberless").ToString(), "0pt", "16pt", "15pt")
            .WithTitleSpacing("subsubsection".ToTag().ToString(), "0pt", "0pt", "25.5pt")
            .WithTitleSpacing(new StringBuilder("name=").WithTag("subsubsection").Append(", numberless").ToString(), "0pt", "16pt", "15pt")
            .AppendLine(@"\newcommand\NB[1][0.3]{N\kern-#1em\textcolor{red}{B}!}");
    }

    private static double GetDistance(Minutia minutia1, Minutia minutia2)
    {
        var manhattan = Math.Abs(minutia1.X - minutia2.X) + Math.Abs(minutia1.Y - minutia2.Y);
        var euclidean = minutia1.DistanceTo(minutia2);
        var minkowski = Math.Pow(Math.Pow(Math.Abs(minutia1.X - minutia2.X), 3) + Math.Pow(Math.Abs(minutia1.Y - minutia2.Y), 3), 1 / 3d);
        return euclidean;
    }

    private static string Part1Latex()
        => @"\part{Cluster details}
            \pagebreak";

    private static string Part2Latex()
        => @"\part{Minutiae comparison}
            \pagebreak";

    private static string AbstractLatexDocument()
        => @"\centering
			In scope of current article we would suppose to use next expression
			\[ S = 
			\begin{pmatrix}
				X_{min} & = & 0 \\
				X_{max} & = & 300 \\
				\mathbb{E}[X] & = & 150\\
				\mathbb{D}[X] & = & 22500 \\
				\sigma & = & 150 \\
				\overline{X} & = & 150 \\
				Q_{3} - Q{1} & = & 22 \\
				\gamma_{1} & = & 0.01 \\
				\gamma_{2} & = & -0.01 \\
			\end{pmatrix}
			\]
			as a structural representation of statistics measurements for population $X$, where\\
			\medskip
			$X_{min}$ and $X_{max}$ - minimum and maximum population values;\\
			$\mathbb{E}[X] = \nu_{1}$ - mathematical expectation;\\
			$\mathbb{D}[X] = \mu_{2}$ - variance\footnote{Assuming here and below to take variance for sample as unbiased with normalizer $\displaystyle \frac{n}{n-1}$};\\
			$\sigma$ - standard deviation\footnote{Assuming here and below to take standard deviation for sample as unbiased with normalizer $\displaystyle \frac{n}{n-1}$};\\
			$\overline{X}$ - median;\\
			$Q_{3}-Q_{1} = IQR$ - interquartile range;\\
			\smallskip
			$\gamma_{1} = \displaystyle \frac{\mu_{3}}{\mu_{2}^{ \frac{3}{2}}}$ - skewness;\\
			\smallskip
			$\gamma_{2} = \displaystyle \frac{\mu_{4}}{\mu_2^2}$ - kurtosis\footnote{For skewness and kurtosis per statistics population formula would include rather division by $\sigma^{n}$, however such formula for sample would produce biased results};\\
			\smallskip
			$\nu_{n}$ - initial moment of distribution;\\
			$\mu_{n}$ - central moment of distribution;";

    private static string ToMinutiaeReport(Cluster cluster)
    {
        var minutiae = cluster.ClusterMinutiaes.Select(x => x.Minutia).OrderBy(x => x.Id).ToList();
        var centroid = cluster.ClusterMinutiaes.First(x => x.IsCentroid).Minutia;
        var leftMinutiae = cluster.ClusterMinutiaes.Where(x => !x.IsCentroid).Select(x => x.Minutia).ToList();
        var pairwiseFirst = leftMinutiae.Select(x => (x.Id, leftMinutiae.Select(y => (y.Id, x.DistanceTo(y)))));
        string[] idRowHeader = ["Id", "-"];
        string[] xRowHeader = ["X", "-"];
        string[] yRowHeader = ["Y", "-"];
        string[] thetaRowHeader = ["Theta", "radians"];
        var coordsTable = ToTable($"Minutiae for cluster {cluster.Id} with centroid {centroid.Id}.",
            $"table:minutiae_{cluster.Id}_coords",
            "centering",
            customTableHeader: @" & Unit & \multicolumn{16}{c|}{}",
            idRowHeader.Concat(minutiae.Select(x => x.Id.ToString())),
            xRowHeader.Concat(minutiae.Select(x => x.X.ToString())),
            yRowHeader.Concat(minutiae.Select(x => x.Y.ToString())),
            thetaRowHeader.Concat(minutiae.Select(x => x.Theta.ToString("F1"))));
        var adjustedCoordsTable = ToTable($"Minutiae for cluster {cluster.Id} with centroid {centroid.Id} after $XY$ normalization (assuming centroid has $X=0$, $Y=0$.",
            $"table:minutiae_{cluster.Id}_adjusted_coords",
            "centering",
            customTableHeader: @" & Unit & \multicolumn{16}{c|}{}",
            idRowHeader.Concat(minutiae.Select(x => x.Id.ToString())),
            xRowHeader.Concat(minutiae.Select(x => (x.X - centroid.X).ToString())),
            yRowHeader.Concat(minutiae.Select(x => (x.Y - centroid.Y).ToString())),
            thetaRowHeader.Concat(minutiae.Select(x => x.Theta.ToString("F1"))));
        var pairwiseTableData = leftMinutiae.OrderBy(x => x.Id)
            .Select(x => new[] { x.Id.ToString() }
                .Concat(pairwiseFirst.First(y => y.Id == x.Id)
                    .Item2
                    .OrderBy(z => z.Id)
                    .Select(z => z.Item2.ToString("F1"))));
        var pairwiseTableHeader = $@" & {string.Join(" & ", leftMinutiae.Select(x => x.Id).Order())}";
        var pairwiseTable = ToTable(pairwiseTableData,
            $"Pairwise distance inside cluster {cluster.Id}.",
            $"table:pairwise_{cluster.Id}",
            "centering",
            customTableHeader: pairwiseTableHeader);

        var template = new StringBuilder(coordsTable)
            .WithLineBreak()
            .Append(adjustedCoordsTable)
            .WithLineBreak()
            .Append(pairwiseTable)
            .WithLineBreak();
        var sample = pairwiseFirst.SelectMany(x => x.Item2.Select(x => x.Item2));
        return template.Append(ToStatisticsFormula(sample, cluster.Id)).ToString();
    }

    private static string ToTable(string caption, string label, string tag, string? customTableHeader = null, params IEnumerable<string>[] items)
        => ToTable(items, caption, label, tag, customTableHeader);

    private static string ToTable(IEnumerable<IEnumerable<string>> lines,
        string caption,
        string label,
        string tag,
        string? customTableHeader = null,
        int? rowColoured = null,
        int? columnColoured = null,
        bool colorAnyCoincidense = false)
    {
        if (!lines.Any() || !lines.All(x => x.Count() == lines.First().Count()))
        {
            throw new Exception();
        }
        var headerFormat = $"|{string.Join("|", Enumerable.Repeat("c", lines.First().Count()))}|";
        var builder = new StringBuilder()
            .WithTag("begin").WithCurlyBrace("table").WithSquareBrace("h!")
            .WithLineBreak()
            .WithTag(tag)
            .WithLineBreak()
            .WithTag("begin").WithCurlyBrace("tabular").WithCurlyBrace(headerFormat)
            .WithLineBreak()
            .WithTag("hline")
            .WithLineBreak();
        if (!string.IsNullOrEmpty(customTableHeader))
        {
            if (columnColoured is not null)
            {
                var splittedHeader = customTableHeader.Split(" & ");
                splittedHeader[columnColoured.Value] = @"\cellcolor{blue!25}" + splittedHeader[columnColoured.Value];
                var header = string.Join(" & ", splittedHeader);
                builder = builder.Append(header).Append(@" \\")
                .WithLineBreak()
                .WithTag("hline")
                .WithLineBreak();
            }
            else
            {
                builder = builder.Append(customTableHeader).Append(@" \\")
                .WithLineBreak()
                .WithTag("hline")
                .WithLineBreak();
            }
        }
        foreach (var (line, i) in lines.Select((x, i) => (x, i)))
        {
            var formattedCell = colorAnyCoincidense
                ? line.Select(x => int.TryParse(x, out var test) && test > 7 ? @"\cellcolor{blue!25}" + x : x)
                : line.Select((x, j) => j == columnColoured ? @"\cellcolor{blue!25}" + x : x);
            var formattedLine = i == rowColoured ? @"\rowcolor{blue!25}" + string.Join(" & ", formattedCell) : string.Join(" & ", formattedCell);
            builder = builder.Append(formattedLine).Append(@" \\")
                .WithLineBreak()
                .WithTag("hline")
                .WithLineBreak();
        }
        return builder.WithTag("end").WithCurlyBrace("tabular")
            .WithLineBreak()
            .WithTag("caption").WithCurlyBrace(caption)
            .WithLineBreak()
            .WithTag("label").WithCurlyBrace(label)
            .WithLineBreak()
            .WithTag("end").WithCurlyBrace("table")
            .WithLineBreak()
            .ToString();
    }

    private static string ToStatisticsFormula(IEnumerable<double> sample, int index, int? index2 = null, string type = "cluster")
    {
        var descriptiveStatistics = new DescriptiveStatistics(sample);
        var median = Statistics.Median(sample);
        var iqr = Statistics.InterquartileRange(sample);
        var values = new Dictionary<string, double>
        {
            { "X_{min}", descriptiveStatistics.Minimum },
            { "X_{max}", descriptiveStatistics.Maximum },
            { @"\mathbb{E}[X]", descriptiveStatistics.Mean },
            { @"\mathbb{D}[X]", descriptiveStatistics.Variance },
            { @"\sigma", descriptiveStatistics.StandardDeviation },
            { @"\overline{X}", median },
            { "Q_3 - Q_1", iqr },
            { @"\gamma_1", descriptiveStatistics.Skewness },
            { @"\gamma_2", descriptiveStatistics.Kurtosis }
        };
        return new StringBuilder().WithTag("begin").WithCurlyBrace("figure").WithSquareBrace("H")
            .WithLineBreak()
            .Append(@"\[")
            .WithLineBreak()
            .Append($"S_{{{index}}} = ")
            .WithLineBreak()
            .Append(ToFormulaMatrix(values))
            .WithLineBreak()
            .Append(@"\]")
            .WithLineBreak()
            .WithTag("caption").WithCurlyBrace($"Statistics for {type} {index} {(index2 is null ? "" : $"and {index2.Value}")}.")
            .WithLineBreak()
            .WithTag("label").WithCurlyBrace($"fig:statistics_{index}{(index2 is null ? "" : $"_{index2}")}")
            .WithLineBreak()
            .WithTag("end").WithCurlyBrace("figure")
            .ToString();
    }

    private static string ToFormulaMatrix(IDictionary<string, double> values)
    {
        var template = new StringBuilder().WithTag("begin").WithCurlyBrace("pmatrix");
        foreach (var (key, value) in values)
        {
            template = template.AppendLine($@"{key} & = & {value:F2} \\");
        }
        return template.WithTag("end").WithCurlyBrace("pmatrix").ToString();
    }
}
