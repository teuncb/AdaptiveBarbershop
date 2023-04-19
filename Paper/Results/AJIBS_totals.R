setwd("D:/Lokaal/Documenten/!GitHub/AdaptiveBarbershop/Paper/Results")
# Load data from C# export
dat = read.csv("params_results.csv", sep=';', dec=',')

# Convert to cents
dat$tieRadius = dat$tieRadius * 100
dat$leadRadius = dat$leadRadius * 100
dat$posterior_drift = dat$posterior_drift * 100
dat$max_retuning = dat$max_retuning * 100
dat$max_deviation = dat$max_deviation * 100

datTies = head(dat, nrow(dat) / 2)
datLead = tail(dat, nrow(dat) / 2)

datTiesT = subset(datTies, datTies$prio == "t")
datTiesL = subset(datTies, datTies$prio == "l")
datLeadT = subset(datLead, datLead$prio == "t")
datLeadL = subset(datLead, datLead$prio == "l")
frames = list(datTiesT, datTiesL, datLeadT, datLeadL)
radStrings = list("tieRange", "tieRange", "leadRange", "leadRange")

pch_drift = 16
pch_ret = 17
pch_dev = 15
pchs = c(pch_drift,pch_ret,pch_dev)

col_drift = "#00C000"
col_ret = "#0000C0"
col_dev = "#C00000"
col_retdev = "#800080"
cols = c(col_drift, col_ret, col_dev)

windowsFonts(Times = windowsFont("Times"))
font_size = 12
title_size = 14

#legend("topright",
#       legend=c("posterior drift (c)", "n of tied note retunings", "n of deviations from ET in lead"),
#       text.col=cols, pch = pchs, col=cols)

par(mar = c(5, 5, 3, 5), font=1, ps=font_size, family="Times")
m <- matrix(c(1,2,3,4,5,5), nrow=3, ncol = 2, byrow = TRUE)
layout(mat = m, heights=c(3.2, 3.2, 1.7))

### TOP LEFT ###

plot(datTiesT$tieRadius, datTiesT$total_drift, pch = pch_drift, col = col_drift,
     ylim=c(1,2.5),
     xlab="", ylab="")
axis(side=2, col=col_drift, col.axis=col_drift)
lines(datTiesT$tieRadius, datTiesT$total_drift, col=col_drift)
abline(h=0, col=col_drift, lty="dashed")

par(new = TRUE)

plot(datTiesT$tieRadius, datTiesT$total_retuning, pch = pch_ret, col = col_ret, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab=""
     )
lines(datTiesT$tieRadius, datTiesT$total_retuning, col = col_ret)

par(new = TRUE)
plot(datTiesT$tieRadius, datTiesT$total_deviation, pch = pch_dev, col = col_dev, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab="", 
     )
lines(datTiesT$tieRadius, datTiesT$total_deviation, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$total_retuning)), col=col_retdev, col.axis=col_retdev)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext(expression("n of audible retunings /" * phantom(" deviations")), side = 4, line = 3, col=col_ret)
mtext(expression(phantom("n of audible retunings /") * " deviations"), side = 4, line = 3, col=col_dev)
mtext("tieRange (c)", side = 1, line = 3)

par(font=2, ps=title_size)
mtext("priority = \"tie\"", side=3, line=1)
par(font=1, ps=font_size)

### TOP RIGHT ###

plot(datTiesL$tieRadius, datTiesL$total_drift, pch = pch_drift, col = col_drift,
     ylim=c(1,2.5),
     xlab="", ylab="")
axis(side=2, col=col_drift, col.axis=col_drift)
lines(datTiesL$tieRadius, datTiesL$total_drift, col=col_drift)
abline(h=0, col=col_drift, lty="dashed")

par(new = TRUE)

plot(datTiesL$tieRadius, datTiesL$total_retuning, pch = pch_ret, col = col_ret,
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab=""
)
lines(datTiesL$tieRadius, datTiesL$total_retuning, col = col_ret)

par(new = TRUE)
plot(datTiesL$tieRadius, datTiesL$total_deviation, pch = pch_dev, col = col_dev, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab="", 
)
lines(datTiesL$tieRadius, datTiesL$total_deviation, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$total_retuning)), col=col_retdev, col.axis=col_retdev)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext(expression("n of audible retunings /" * phantom(" deviations")), side = 4, line = 3, col=col_ret)
mtext(expression(phantom("n of audible retunings /") * " deviations"), side = 4, line = 3, col=col_dev)
mtext("tieRange (c)", side = 1, line = 3)

par(font=2, ps=title_size)
mtext("priority = \"lead\"", side=3, line=1)
par(font=1, ps=font_size)


### BOTTOM LEFT ###

plot(datLeadT$leadRadius, datLeadT$total_drift, pch = pch_drift, col = col_drift,
     ylim=c(1,2.5),
     xlab="", ylab="")
axis(side=2, col=col_drift, col.axis=col_drift)
lines(datLeadT$leadRadius, datLeadT$total_drift, col=col_drift)
abline(h=0, col=col_drift, lty="dashed")

par(new = TRUE)

plot(datLeadT$leadRadius, datLeadT$total_retuning, pch = pch_ret, col = col_ret, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab=""
)
lines(datLeadT$leadRadius, datLeadT$total_retuning, col = col_ret)

par(new = TRUE)
plot(datLeadT$leadRadius, datLeadT$total_deviation, pch = pch_dev, col = col_dev, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab="", 
)
lines(datLeadT$leadRadius, datLeadT$total_deviation, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$total_retuning)), col=col_retdev, col.axis=col_retdev)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext(expression("n of audible retunings /" * phantom(" deviations")), side = 4, line = 3, col=col_ret)
mtext(expression(phantom("n of audible retunings /") * " deviations"), side = 4, line = 3, col=col_dev)
mtext("leadRange (c)", side = 1, line = 3)


### BOTTOM RIGHT ###

plot(datLeadL$leadRadius, datLeadL$total_drift, pch = pch_drift, col = col_drift,
     ylim=c(1,2.5),
     xlab="", ylab="")
axis(side=2, col=col_drift, col.axis=col_drift)
lines(datLeadL$leadRadius, datLeadL$total_drift, col=col_drift)
abline(h=0, col=col_drift, lty="dashed")

par(new = TRUE)

plot(datLeadL$leadRadius, datLeadL$total_retuning, pch = pch_ret, col = col_ret, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab=""
)
lines(datLeadL$leadRadius, datLeadL$total_retuning, col = col_ret)

par(new = TRUE)
plot(datLeadL$leadRadius, datLeadL$total_deviation, pch = pch_dev, col = col_dev, 
     ylim=c(0,5),
     axes = FALSE, xlab ="", ylab="", 
)
lines(datLeadL$leadRadius, datLeadL$total_deviation, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$total_retuning)), col=col_retdev, col.axis=col_retdev)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext(expression("n of audible retunings /" * phantom(" deviations")), side = 4, line = 3, col=col_ret)
mtext(expression(phantom("n of audible retunings /") * " deviations"), side = 4, line = 3, col=col_dev)
mtext("leadRange (c)", side = 1, line = 3)


### LEGEND ###
plot(1, type = "n", axes=FALSE, xlab="", ylab="")

legend("bottom",legend = c("posterior drift (c)", "n of tied note retunings", "n of deviations from ET in lead intervals"), 
       col = cols, pch = pchs, horiz = TRUE, lwd=1, bty='n')

par(ps = title_size + 5)
title(main="Proposed algorithm's effects on Ring-A Ding Ding")
par(ps = font_size)
