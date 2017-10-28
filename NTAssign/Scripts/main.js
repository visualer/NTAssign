function validate(form) {
    if (form.Val1.value === "" && form.Val2.value === "" ||
        form.Val1.value === "" && form.RBM.value === "" ||
        form.Val2.value === "" && form.RBM.value === "") {
        $("#hint").addClass("alert-danger");
        $("#hint").html("<span class=\"glyphicon glyphicon-exclamation-sign\" aria-hidden=\"true\"></span> At least 2 values are required.");
        return false;
    }
    else {
        var val1_ = parseInt(form.Val1.value);
        var val2_ = parseInt(form.Val2.value);
        var rbm_ = parseInt(form.RBM.value);
        if (!isNaN(val1_) && (val1_ < 0 || val1_ > 4) ||
            !isNaN(val2_) && (val2_ < 0 || val2_ > 4) ||
            !isNaN(rbm_) && (rbm_ < 30 || rbm_ > 500)) {
            $("#hint").addClass("alert-danger");
            $("#hint").html("<span class=\"glyphicon glyphicon-exclamation-sign\" aria-hidden=\"true\"></span> Please input valid energy value.");
            return false;
        }
        if ($("#slP1").selectpicker("val") === "") {
            $("#hint").addClass("alert-danger");
            $("#hint").html("<span class=\"glyphicon glyphicon-exclamation-sign\" aria-hidden=\"true\"></span> Please select the type of transition energy.");
            return false;
        }
        $(".selectpicker").removeAttr("disabled");
        // do not refresh the selectpicker. only for posting valid data.
        return true;
    }
}

/*function page2Load(env) {
    var thres, $slp = $(".slp");
    $slp.find("option").removeAttr("selected");
    $slp.selectpicker("refresh");
    $slp.each(function () {
        $(this).change(function () {
            var $another, li, $t = $(this);
            if (this.id === "slP1")
                $another = $("#slP2");
            else
                $another = $("#slP1");
            var i = parseInt($t.val());
            $another.find("option").each(function () {
                var $opt = $(this), val = parseInt($opt.val());
                if (i % 4 < 2) {
                    if (val % 4 === 2 || val % 4 === 3)
                        $opt.attr("disabled", "disabled");
                    else $opt.removeAttr("disabled");
                }
                else {
                    if (val % 4 === 0 || val % 4 === 1)
                        $opt.attr("disabled", "disabled");
                    else $opt.removeAttr("disabled");
                }
                if (val === i)
                    $opt.attr("disabled", "disabled");
            });
            $t.find("option").each(function () {
                $(this).removeAttr("disabled");
            });
            $slp.selectpicker("refresh");
        });
    });
}*/

//line 826 title
//line ~735 selected
function clearTitle() {
    $(".bootstrap-select").find("button").removeAttr("title");
}
function regClear() {
    $("select").on("loaded.bs.select", clearTitle);
}

function page1Load() {
    regClear();
    $("#selectEnv").on('changed.bs.select', clearTitle);
}

function page2Load() {
    //document.formInput.reset();
    var $slp = $(".slp"), $another = $("#slP2");
    $slp.find("option").removeAttr("selected");
    $slp.selectpicker("refresh");
    regClear();

    $("#slP1").on('changed.bs.select', function (e, index) {
        var i = index - 1;
        clearTitle();
        $another.find("option").each(function () {
            var $opt = $(this), value = parseInt($opt.val());
            if (i % 2 === 0 && value === i + 1 || i % 2 === 1 && value === i - 1)
                $another.selectpicker('val', value);
        });
    });
}

function drawPlot(placeholder, params) {
    var defaultRadius = 4, xmin = params.point[0] - 0.5, xmax = params.point[0] + 0.5;
    var ymin = params.isMetal ? -0.01 : params.point[1] - 0.4, ymax = params.isMetal ? 0.45 : params.point[1] + 0.4;
    var font = {
        size: 20,
        lineHeight: 20,
        family: "serif"
    };
    var options = {
        xaxis: {
            // aver
            show: true,
            position: "bottom",
            min: xmin,
            max: xmax,
            font: font,
            tickLength: 10,
            tickColor: "rgb(255, 0, 0)",
            color: "rgb(255, 0, 0)"
        },
        yaxis: {
            // PLOTTING PARAMETERS ALSO OCCUR IN plotmodel.cs
            show: true,
            min: ymin,
            max: ymax,
            font: font,
            tickLength: 10,
            tickColor: "rgb(0, 0, 0)", 
            color: "rgb(0, 0, 0)"
        },
        series: {
            hoverable: true
            // shadowSize: 0
        },
        grid: {
            borderColor: {
                top: "rgb(0, 0, 255)",
                bottom: "rgb(255, 0, 0)",
                left: "rgb(0, 0, 0)",
                right: "rgb(0, 0, 0)"
            },
            hoverable: true,
            minBorderMargin: 0
        }
    };
    
    var series = [], pointColor;
    for (var i = 0; i < params.rbm.length; i++)
        series.push({
            color: "rgb(208, 208, 208)", // use the shadow
            data: params.rbm[i],
            shadowSize: 5,
            hoverable: false
        });
    var laser = [1240 / 785, 1240 / 633, 1240 / 532];
    var laser_color = ["rgb(136, 136, 136)", "rgb(245, 0, 0)", "rgb(12, 127, 15)"];

    for (i = 0; i < laser.length; i++) {
        series = series.concat([{
            id: "laser" + i.toString(),
            color: laser_color[i],
            data: [[-10, 2 * (-10 - laser[i])], [10, 2 * (10 - laser[i])]],
            shadowSize: 0
        }, {
            id: "laser_minus" + i.toString(),
            color: laser_color[i],
            data: [[-10, -2 * (-10 - laser[i])], [10, -2 * (10 - laser[i])]],
            shadowSize: 0
        }, {
            fillBetween: "laser" + i.toString(),
            color: laser_color[i],
            data: [[-10, 2 * (-10 - (laser[i] - 0.1))], [10, 2 * (10 - (laser[i] - 0.1))]], // 100 meV
            lines: {
                fill: 0.3,
                lineWidth: 0
            }
        }, {
            fillBetween: "laser_minus" + i.toString(),
            color: laser_color[i],
            data: [[-10, -2 * (-10 - (laser[i] - 0.1))], [10, -2 * (10 - (laser[i] - 0.1))]], // 100 meV
            lines: {
                fill: 0.3,
                lineWidth: 0
            }
        }, {
            fillBetween: "laser" + i.toString(),
            color: laser_color[i],
            data: [[-10, 2 * (-10 - (laser[i] + 0.1))], [10, 2 * (10 - (laser[i] + 0.1))]], // 200 meV
            lines: {
                fill: 0.3,
                lineWidth: 0
            }
        }, {
            fillBetween: "laser_minus" + i.toString(),
            color: laser_color[i],
            data: [[-10, -2 * (-10 - (laser[i] + 0.1))], [10, -2 * (10 - (laser[i] + 0.1))]], // 200 meV
            lines: {
                fill: 0.3,
                lineWidth: 0
            }
        }]);
    }

    for (i = 0; i < params.all.length; i++) {
        var mod_i = (params.all_label[i][0][0] * 2 + params.all_label[i][0][1]) % 3;
        series.push({
            color: params.isMetal || mod_i === 2 ? "rgb(0, 0, 0)" : "rgb(255, 0, 0)",
            data: params.all[i],
            points: {
                show: true,
                radius: params.isMetal ? 78 / 0.1 * 0.015 / 2  : defaultRadius, //15 meV
                symbol: params.isMetal ? "circle" : mod_i === 2 ? "square" : "triangle"
            },
            lines: {
                show: true
            },
            point_labels: params.all_label[i],
            hoverable: true
        });
    }
    for (i = 0; i < params.result.length; i++) {
        mod_i = (params.result_label[i][0] * 2 + params.result_label[i][1]) % 3;
        series.push({
            color: params.isMetal || mod_i === 2 ? "rgb(0, 0, 0)" : "rgb(255, 0, 0)",
            data: [params.result[i]],
            points: {
                show: true,
                radius: params.isMetal ? 78 / 0.1 * 0.015 / 2 : defaultRadius,
                symbol: params.isMetal ? "circle" : mod_i === 2 ? "square" : "triangle",
                fill: true,
                fillColor: params.isMetal || mod_i === 2 ? "rgb(0, 0, 0)" : "rgb(255, 0, 0)"
            },
            point_labels: [params.result_label[i]],
            hoverable: true
        });
    }

    if (params.pointType === "red")
        pointColor = "rgb(255, 0, 0)";
    else if (params.pointType === "green")
        pointColor = "rgb(12, 180, 15)";
    // blue point is diamod, otherwise square
    series.push({
        color: pointColor,
        data: [params.point],
        points: {
            show: true,
            symbol: params.pointType === "green" ? "diamond" : "square",
            radius: defaultRadius,
            fill: true,
            fillColor: params.pointType === "green" ? "rgba(0, 0, 0, 0)" : pointColor
        },
        hoverable: false
    });
    if (params.bluePoint !== null)
        series.push({
            color: "rgb(0, 0, 255)",
            data: [params.bluePoint],
            points: {
                show: true,
                symbol: "diamond",
                radius: defaultRadius,
                fill: true,
                fillColor: "rgba(0, 0, 0, 0)"
            },
            hoverable: false,
            shadowSize: 0
        });



    var $placeholder = $("#" + placeholder);
    var plot = $.plot($placeholder, series, options);


    var previousPoint = null;
    $placeholder.bind("plothover", function(event, pos, item) {
        if (item) {
            if (previousPoint !== item.dataIndex) {
                previousPoint = item.dataIndex;
                $("#tooltip").remove();
                showTooltip(item.pageX, item.pageY,
                    "(" + item.series.point_labels[item.dataIndex][0] + ", " +
                    item.series.point_labels[item.dataIndex][1] + ")");
            }
        } else {
            $("#tooltip").remove();
            previousPoint = null;
        }
    });

    if (params.isMetal) {
        for (i = 0; i < params.all.length; i++) {
            var s = Math.floor(params.all.length / 2);
            var p = params.all[i][params.all[i].length - 1];
            var p1 = params.all_label[i][params.all[i].length - 1];
            var o = plot.pointOffset({ x: p[0], y: p[1] });
            var axes = plot.getAxes();
            var xaxis = axes.xaxis, yaxis = axes.yaxis;
            if (i % 2 === s % 2 && p[0] <= xaxis.max && p[0] >= xaxis.min && p[1] <= yaxis.max - 0.05 && p[1] >= yaxis.min + 0.05)
                $placeholder.append("<div class='series_label' style='text-align:center;font-size:20px;position:absolute;left:" + (o.left - (i === s ? 60 : 10)) +
                    "px;top:" + (o.top - 30) + "px;'><p>" +
                    (i === s ? "2<i>n</i>+<i>m</i>=" : "") + (p1[0] * 2 + p1[1]) + "</p></div>"
                );
        }
    } else {
        var mid = parseInt(params.all.length / 2);
        var pMid = params.all_label[mid][params.all[mid].length - 1];
        var divMid = Math.floor((pMid[0] * 2 + pMid[1]) / 3); //use ceil, for right is scattered, left is dense

        for (i = 0; i < params.all.length; i++) {
            p = params.all[i][params.all[i].length - 1];
            p1 = params.all_label[i][params.all[i].length - 1];
            o = plot.pointOffset({ x: p[0], y: p[1] });
            axes = plot.getAxes();
            xaxis = axes.xaxis, yaxis = axes.yaxis;
            div = parseInt((p1[0] * 2 + p1[1]) / 3);
            var mod = (p1[0] * 2 + p1[1]) % 3;
            if (divMid % 2 === div % 2) {
                if (p[0] <= xaxis.max - 0.02 && p[0] >= xaxis.min + 0.02 && p[1] <= yaxis.max - 0.1 && p[1] >= yaxis.min + 0.1)
                    $placeholder.append("<div class='series_label' style='text-align:center;font-size:20px;position:absolute;" +
                        "color:" + (mod === 1 ? "#FF0000" : "#000000") +
                        ";left:" + 
                        (o.left - (divMid === div ? 60 : 10)) +
                        "px;top:" + (o.top - (mod === 1 ? -15 : divMid === div && p[1] <= yaxis.max - 0.15 && p[1] >= yaxis.min + 0.18 ? 60 : 30)) + "px;'><p>" +
                        (divMid === div ? (mod === 2 && p[1] <= yaxis.max - 0.15 ? "<b>MOD2</b><br/>" : "") +
                            "2<i>n</i>+<i>m</i>=" : "") +
                        (p1[0] * 2 + p1[1]) +
                        (divMid === div && mod === 1 && p[1] >= yaxis.min + 0.12? "<br/><b>MOD1</b>" : "") +
                        "</p></div>" 
                    );
            }
        }
    }
    for (i = 0; i < params.rbm.length; i++) {
        if (i % 2 === 0 || params.rbm.length === 1) {
            var o1 = plot.pointOffset({ x: params.rbm_pos[i], y: ymax });
            $placeholder.append("<div class='axis_label' style='color:#0000FF;font-size:20px;position:absolute;left:" + (o1.left - 15) +
                "px;top:" + (o1.top - 30) + "px;'><p>" +
                params.rbm_label[i] + "</p></div>"
            );
        }
    }
}

function showTooltip(x, y, contents) {
    $("<div id='tooltip' class='tooltip right in'><div class='tooltip-arrow' style='top: 50%;'>" +
        "</div><div class='tooltip-inner'>" + contents + "</div></div>").css({
            display: "block",
            top: y,
            left: x
        }).appendTo("body").fadeIn(200);
}

