setwd("D:/Lokaal/Documenten/!GitHub/AdaptiveBarbershop/Paper/Results")
# Load data from C# export
dat = read.csv("params_results.csv", sep=';', dec=',')

# Convert to cents
dat$tieRadius = dat$tieRadius * 100
dat$leadRadius = dat$leadRadius * 100
dat$posterior_drift = dat$posterior_drift * 100
dat$max_retuning = dat$max_retuning * 100
dat$max_deviation = dat$max_deviation * 100

datTies = head(dat, 30)
datLead = tail(dat, 30)

datTiesT = subset(datTies, datTies$prio == "t")
datTiesL = subset(datTies, datTies$prio == "l")
datLeadT = subset(datLead, datLead$prio == "t")
datLeadL = subset(datLead, datLead$prio == "l")
frames = list(datTiesT, datTiesL, datLeadT, datLeadL)
radStrings = list("tieRadius", "tieRadius", "leadRadius", "leadRadius")

pch_drift = 16
pch_ret = 17
pch_dev = 15
pchs = c(pch_drift,pch_ret,pch_dev)

col_drift = "#C00000"
col_ret = "#0000C0"
col_dev = "#00C000"
cols = c(col_drift, col_ret, col_dev)

#legend("topright",
#       legend=c("posterior drift (c)", "n of tied note retunings", "n of deviations from ET in lead"),
#       text.col=cols, pch = pchs, col=cols)

par(mar = c(5, 5, 3, 5))
m <- matrix(c(1,2,3,4,5,5), nrow=3, ncol = 2, byrow = TRUE)
layout(mat = m, heights=c(3, 3, 1.8))

### TOP LEFT ###

plot(datTiesT$tieRadius, datTiesT$posterior_drift, pch = pch_drift, col = col_drift,
     # xlim=c(0,0.3), ylim=c(0.2,0.5),
     xlab="", ylab="")
lines(datTiesT$tieRadius, datTiesT$posterior_drift, col=2)

par(new = TRUE)

plot(datTiesT$tieRadius, datTiesT$n_retunings, pch = pch_ret, col = col_ret, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datTiesT$tieRadius, datTiesT$n_retunings, col = col_ret)

par(new = TRUE)
plot(datTiesT$tieRadius, datTiesT$n_deviations, pch = pch_dev, col = col_dev, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datTiesT$tieRadius, datTiesT$n_deviations, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$n_retunings)), col=col_ret, col.axis=col_ret)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext("n of audible retunings / deviations", side = 4, line = 3, col=col_ret)
mtext("tieRadius (c)", side = 1, line = 3)

### TOP RIGHT ###

plot(datTiesL$tieRadius, datTiesL$posterior_drift, pch = pch_drift, col = col_drift,
     # xlim=c(0,0.3), ylim=c(0.2,0.5),
     xlab="", ylab="")
lines(datTiesL$tieRadius, datTiesL$posterior_drift, col=2)

par(new = TRUE)

plot(datTiesL$tieRadius, datTiesL$n_retunings, pch = pch_ret, col = col_ret, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datTiesL$tieRadius, datTiesL$n_retunings, col = col_ret)

par(new = TRUE)
plot(datTiesL$tieRadius, datTiesL$n_deviations, pch = pch_dev, col = col_dev, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datTiesL$tieRadius, datTiesL$n_deviations, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$n_retunings)), col=col_ret, col.axis=col_ret)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext("n of audible retunings / deviations", side = 4, line = 3, col=col_ret)
mtext("tieRadius (c)", side = 1, line = 3)


### BOTTOM LEFT ###

plot(datLeadT$leadRadius, datLeadT$posterior_drift, pch = pch_drift, col = col_drift,
     # xlim=c(0,0.3), ylim=c(0.2,0.5),
     xlab="", ylab="")
lines(datLeadT$leadRadius, datLeadT$posterior_drift, col=2)

par(new = TRUE)

plot(datLeadT$leadRadius, datLeadT$n_retunings, pch = pch_ret, col = col_ret, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datLeadT$leadRadius, datLeadT$n_retunings, col = col_ret)

par(new = TRUE)
plot(datLeadT$leadRadius, datLeadT$n_deviations, pch = pch_dev, col = col_dev, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datLeadT$leadRadius, datLeadT$n_deviations, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$n_retunings)), col=col_ret, col.axis=col_ret)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext("n of audible retunings / deviations", side = 4, line = 3, col=col_ret)
mtext("leadRadius (c)", side = 1, line = 3)


### BOTTOM RIGHT ###

plot(datLeadL$leadRadius, datLeadL$posterior_drift, pch = pch_drift, col = col_drift,
     # xlim=c(0,0.3), ylim=c(0.2,0.5),
     xlab="", ylab="")
lines(datLeadL$leadRadius, datLeadL$posterior_drift, col=2)

par(new = TRUE)

plot(datLeadL$leadRadius, datLeadL$n_retunings, pch = pch_ret, col = col_ret, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datLeadL$leadRadius, datLeadL$n_retunings, col = col_ret)

par(new = TRUE)
plot(datLeadL$leadRadius, datLeadL$n_deviations, pch = pch_dev, col = col_dev, 
     axes = FALSE, xlab ="", ylab="", ylim=c(0,40))
lines(datLeadL$leadRadius, datLeadL$n_deviations, col = col_dev)
axis(side=4, at = pretty(range(datTiesT$n_retunings)), col=col_ret, col.axis=col_ret)
mtext("posterior drift (c)", col = col_drift, side = 2, line = 3)
mtext("n of audible retunings / deviations", side = 4, line = 3, col=col_ret)
mtext("leadRadius (c)", side = 1, line = 3)


### LEGEND ###
plot(1, type = "n", axes=FALSE, xlab="", ylab="")

legend("bottom",legend = c("posterior drift (c)", "n of tied note retunings", "n of deviations from ET in lead"), 
       col = cols, pch = pchs, horiz = TRUE, lwd=1)

title(main="Proposed algorithm's effects on Ring-A Ding Ding")
